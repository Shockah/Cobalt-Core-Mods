using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.BetterCodex;

internal sealed class CrewCodex : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CodexProgress), nameof(CodexProgress.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CodexProgress_Render_Postfix))
		);
	}

	private static void CodexProgress_Render_Postfix(G g)
	{
		if (g.metaRoute?.subRoute is not Codex codex)
			return;
		codex.subRoute = new CrewCodexRoute { DeckType = ModEntry.Instance.Helper.Content.Decks.LookupByDeck(NewRunOptions.allChars[Mutil.NextRandInt() % NewRunOptions.allChars.Count])!.UniqueName };
	}

	internal sealed class CrewCodexRoute : Route, OnMouseDown, OnInputPhase
	{
		public required string DeckType;

		private IDeckEntry? DeckEntryStorage;
		private IPlayableCharacterEntryV2? CharacterEntryStorage;
		private Character? CharacterStorage;
		private List<Card>? CardsStorage;
		
		private readonly State FakeState;
		private readonly Combat FakeCombat;

		private Card? HoveredCard;

		public CrewCodexRoute()
		{
			FakeState = State.FakeState();
			FakeCombat = Combat.Make(FakeState, new FakeCombatForCardRewards(), doForReal: false);
		}

		private IDeckEntry? GetDeckEntry()
		{
			DeckEntryStorage ??= ModEntry.Instance.Helper.Content.Decks.LookupByUniqueName(DeckType);
			return DeckEntryStorage;
		}

		private IPlayableCharacterEntryV2? GetCharacterEntry()
		{
			if (CharacterEntryStorage is { } existingValue)
				return existingValue;
			if (GetDeckEntry() is not { } deckEntry)
				return null;

			CharacterEntryStorage = ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(deckEntry.Deck);
			return CharacterEntryStorage;
		}

		private Character? GetCharacter()
		{
			if (CharacterStorage is { } existingValue)
				return existingValue;
			if (GetCharacterEntry() is not { } characterEntry)
				return null;

			CharacterStorage = new Character { type = characterEntry.CharacterType, deckType = characterEntry.Configuration.Deck };
			return CharacterStorage;
		}

		private List<Card>? GetCards()
		{
			if (CardsStorage is { } existingValue)
				return existingValue;
			if (GetCharacterEntry() is not { } characterEntry)
				return null;

			CardsStorage = DB.cards
				.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.cardMetas[kvp.Key]))
				.Where(e => e.Meta.deck == characterEntry.Configuration.Deck)
				.Where(e => !e.Meta.unreleased)
				.Select(e => (Card)Activator.CreateInstance(e.Type)!)
				.ToList();
			return CardsStorage;
		}
		
		public override void Render(G g)
		{
			const int margin = 120;
			const int cardSpacing = 17;
			
			if (GetCharacterEntry() is not { } characterEntry || GetCharacter() is not { } character || GetCards() is not { } cards)
			{
				g.CloseRoute(this);
				return;
			}

			var lastHoveredCard = HoveredCard;
			HoveredCard = null;

			FakeState.characters.Clear();
			FakeState.characters.Add(character);

			g.Push(onInputPhase: this);
			
			SharedArt.DrawEngineering(g);

			Draw.Text(Loc.T($"char.{characterEntry.Configuration.Deck}.desc") ?? "", margin + 68, 26, color: Colors.textMain, outline: Colors.black, maxWidth: g.mg.PIX_W - 68 - margin * 2);

			var commonCards = cards.Where(card => card.GetMeta() is { dontOffer: false, rarity: Rarity.common }).OrderBy(card => card.GetFullDisplayName()).ToList();
			var uncommonCards = cards.Where(card => card.GetMeta() is { dontOffer: false, rarity: Rarity.uncommon }).OrderBy(card => card.GetFullDisplayName()).ToList();
			var rareCards = cards.Where(card => card.GetMeta() is { dontOffer: false, rarity: Rarity.rare }).OrderBy(card => card.GetFullDisplayName()).ToList();
			var otherCards = cards.Where(card => card.GetMeta().dontOffer).OrderBy(card => card.GetMeta().rarity).ThenBy(card => card.GetFullDisplayName()).ToList();

			RenderCards(commonCards, margin - 1, 100);
			RenderCards(uncommonCards, margin - 1 + 62, 100);
			RenderCards(rareCards, margin - 1 + 124, 100);
			RenderCards(otherCards, margin - 1 + 186, 100);
			
			// render last, to make sure shouts don't render behind
			character.Render(g, margin, 24, showTooltips: false, onMouseDown: this);

			g.Pop();

			void RenderCards(List<Card> cards, int x, int y)
			{
				var selectedCardIndex = -1;
				
				for (var i = 0; i < cards.Count; i++)
				{
					if (lastHoveredCard == cards[i])
					{
						selectedCardIndex = i;
						continue;
					}
					
					RenderCard(i);
				}

				if (selectedCardIndex != -1)
					RenderCard(selectedCardIndex);

				void RenderCard(int index)
				{
					var card = cards[index];
					card.drawAnim = 1;
					card.pos = card.targetPos = new Vec(x, y + index * cardSpacing);
					card.Render(g, fakeState: FakeState, hideFace: !card.IsDiscovered(g.state));

					if (g.boxes.LastOrDefault(box => box.key == card.UIKey()) is not { } box)
						return;
					
					if (index != cards.Count - 1)
						box.rect.h = Math.Min(box.rect.h, cardSpacing);
					if (box.IsHover())
						HoveredCard = card;
				}
			}
		}

		public void OnMouseDown(G g, Box b)
		{
			if (GetDeckEntry() is not { } deckEntry || GetCharacter() is not { } character)
				return;

			if (b.key?.k == StableUK.character && b.key!.Value.v == (int)deckEntry.Deck)
			{
				if (GetRandomLine() is { } line)
				{
					var oldState = g.state;
					try
					{
						g.state = FakeState;
						line.Line.Execute(g, FakeCombat, line.Context);
					}
					finally
					{
						g.state = oldState;
					}
				}

				return;
			}
		}

		public void OnInputPhase(G g, Box b)
		{
			if (Input.GetGpDown(Btn.B) || Input.GetKeyDown(Keys.Escape))
			{
				g.CloseRoute(this);
				return;
			}
		}

		private (ScriptCtx Context, Say Line)? GetRandomLine()
		{
			if (GetCharacterEntry() is not { } characterEntry)
				return null;

			var characterLines = GetCharacterLines().ToList();
			if (characterLines.Count == 0)
				return null;

			return characterLines[Mutil.NextRandInt() % characterLines.Count];

			IEnumerable<(ScriptCtx Context, Say Line)> GetCharacterLines()
			{
				foreach (var (script, storyNode) in DB.story.all)
				{
					for (var i = 0; i < storyNode.lines.Count; i++)
					{
						var line = storyNode.lines[i];
						foreach (var flatLine in GetFlattenedLines(line))
						{
							if (flatLine.who != characterEntry.CharacterType)
								continue;
							yield return (new ScriptCtx { script = script, idx = i }, flatLine);
						}
					}
				}
			}

			IEnumerable<Say> GetFlattenedLines(Instruction instruction)
			{
				if (instruction is Say say)
				{
					yield return say;
				} 
				else if (instruction is SaySwitch saySwitch)
				{
					foreach (var say2 in saySwitch.lines)
						yield return say2;
				}
			}
		}
	}
}