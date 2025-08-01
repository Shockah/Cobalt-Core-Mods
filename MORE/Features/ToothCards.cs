using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Shockah.Kokoro;

namespace Shockah.MORE;

internal sealed class ToothCards : IRegisterable
{
	private static ICardEntry FiddleCardEntry = null!;
	private static ICardEntry SlipCardEntry = null!;
	private static ICardEntry FinalFormCardEntry = null!;
	private static ICardEntry SkimCardEntry = null!;
	private static ICardEntry SmashCardEntry = null!;
	private static ICardEntry FidgetCardEntry = null!;
	private static ICardEntry ToothExeCardEntry = null!;

	internal static readonly string[] VanillaCardKeys = [
		nameof(Buckshot), nameof(WaltzCard), nameof(BruiseCard), nameof(LightningBottle)
	];

	internal static string[] ModdedCardKeys = null!;
	internal static string[] AllToothCardKeys = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		FiddleCard.RegisterCard(helper);
		SlipCard.RegisterCard(helper);
		FinalFormCard.RegisterCard(helper);
		SkimCard.RegisterCard(helper);
		SmashCard.RegisterCard(helper);
		FidgetCard.RegisterCard(helper);
		ToothExeCard.RegisterCard(helper);

		ModdedCardKeys = [
			FiddleCardEntry.UniqueName, SlipCardEntry.UniqueName, FinalFormCardEntry.UniqueName,
			SkimCardEntry.UniqueName, SmashCardEntry.UniqueName, FidgetCardEntry.UniqueName,
		];

		AllToothCardKeys = [
			.. VanillaCardKeys,
			.. ModdedCardKeys,
		];

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(MapEvent), nameof(MapEvent.MakeRoute)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapEvent_MakeRoute_Prefix)))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.ToothCardOffering)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Events_ToothCardOffering_Postfix)))
		);
	}

	private static void MapEvent_MakeRoute_Prefix(State s)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(s, "ToothChoicesPage");

	private static void Events_ToothCardOffering_Postfix(ref List<Choice> __result)
	{
		if (__result.Find(choice => choice.label == Loc.T("ToothCardOffering_No")) is not { } noChoice)
			return;

		__result = [
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "choice"]),
				key = "ToothCardOffering_After",
				actions = [new PickAToothCardAction()],
			},
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "choiceExe"]),
				key = "ToothCardOffering_After",
				actions = [new AAddCard { card = new ToothExeCard(), callItTheDeckNotTheDrawPile = true }],
			},
			noChoice,
		];
	}

	private sealed class ToothCardsBrowseSource : IKokoroApi.IV2.ICustomCardBrowseSourceApi.ICustomCardBrowseSource
	{
		private readonly Lazy<List<Card>> SortedVanillaCards = new(
			() => VanillaCardKeys
				.Where(key => !ModEntry.Instance.Settings.ProfileBased.Current.DisabledToothCards.Contains(key))
				.Select(key => (Card)Activator.CreateInstance(DB.cards[key])!)
				.OrderBy(card => card.GetFullDisplayName())
				.ToList()
		);
		
		private readonly Lazy<List<Card>> SortedModdedCards = new(
			() => ModdedCardKeys
				.Where(key => !ModEntry.Instance.Settings.ProfileBased.Current.DisabledToothCards.Contains(key))
				.Select(key => (Card)Activator.CreateInstance(DB.cards[key])!)
				.OrderBy(card => card.GetFullDisplayName())
				.ToList()
		);
		
		private readonly Lazy<List<Card>> Cards;

		public ToothCardsBrowseSource()
		{
			this.Cards = new(() => [.. SortedVanillaCards.Value, .. SortedModdedCards.Value]);
		}
		
		public IReadOnlyList<Tooltip> GetSearchTooltips(State state)
			=> [
				new GlossaryTooltip("action.searchCardNew")
				{
					Icon = StableSpr.icons_searchCardNew,
					TitleColor = Colors.action,
					Title = Loc.T("action.searchCardNew.name"),
					Description = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "choiceSearchTooltip"]),
				},
			];

		public string GetTitle(State state, Combat combat, IReadOnlyList<Card> cards)
			=> ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "choiceBrowseSourceTitle"], new { Count = cards.Count });

		public IReadOnlyList<Card> GetCards(State state, Combat combat)
			=> Cards.Value;
	}

	private sealed class PickAToothCardAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> new ToothCardsBrowseSource().GetSearchTooltips(s).ToList();

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var route = ModEntry.Instance.KokoroApi.CustomCardBrowseSource.ModifyCardBrowse(new CardBrowse
			{
				mode = CardBrowse.Mode.Browse,
				browseAction = new AddCardToDeckAction(),
				allowCancel = true,
			}).SetCustomBrowseSource(new ToothCardsBrowseSource()).AsRoute;
			if (route.GetCardList(g).Count == 0)
			{
				timer = 0;
				return null;
			}
			return route;
		}
	}
	
	private sealed class AddCardToDeckAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			if (selectedCard is null)
				return;
			
			s.RemoveCardFromWhereverItIs(selectedCard.uuid);
			s.SendCardToDeck(selectedCard);
		}

		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "choiceUiTitle"]);
	}

	[UsedImplicitly]
	private sealed class FiddleCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			FiddleCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Fiddle", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new() { cost = 0, art = StableSpr.cards_riggs, description = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fiddle", "description", upgrade.ToString()]) };

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new ACardSelect { browseSource = CardBrowse.Source.Hand, browseAction = new DiscardAndDrawAction() }
				],
				Upgrade.A => [
					new ACardSelect { browseSource = CardBrowse.Source.Hand, browseAction = new RedrawAction { Times = 2 } }
				],
				_ => [
					new ACardSelect { browseSource = CardBrowse.Source.Hand, browseAction = new RedrawAction() }
				]
			};

		private sealed class RedrawAction : CardAction
		{
			public int Times = 1;

			public override string GetCardSelectText(State s)
				=> ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fiddle", "drawACardInYourHand"]);

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				if (selectedCard is null || s.FindCard(selectedCard.uuid) is not { } card)
				{
					timer = 0;
					return;
				}

				s.RemoveCardFromWhereverItIs(selectedCard.uuid);
				c.SendCardToHand(s, card);
				Audio.Play(Event.CardHandling);
					
				foreach (var artifact in s.EnumerateAllArtifacts())
					artifact.OnDrawCard(s, c, 1);

				if (Times > 1)
					c.QueueImmediate(new RedrawAction { selectedCard = selectedCard, Times = Times - 1 });
			}
		}

		private sealed class DiscardAndDrawAction : CardAction
		{
			public override string GetCardSelectText(State s)
				=> ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fiddle", "discardAndDrawACardInYourHand"]);

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				if (selectedCard is null || s.FindCard(selectedCard.uuid) is not { } card)
				{
					timer = 0;
					return;
				}

				s.RemoveCardFromWhereverItIs(card.uuid);
				c.SendCardToDiscard(s, card);
				Audio.Play(Event.CardHandling);

				c.QueueImmediate(new DrawBackAction { selectedCard = selectedCard });
			}

			private sealed class DrawBackAction : CardAction
			{
				public override void Begin(G g, State s, Combat c)
				{
					base.Begin(g, s, c);
					if (selectedCard is null || s.FindCard(selectedCard.uuid) is not { } card)
					{
						timer = 0;
						return;
					}

					s.RemoveCardFromWhereverItIs(card.uuid);
					c.SendCardToHand(s, card);
					Audio.Play(Event.CardHandling);
					
					foreach (var artifact in s.EnumerateAllArtifacts())
						artifact.OnDrawCard(s, c, 1);
				}
			}
		}
	}

	[UsedImplicitly]
	private sealed class SlipCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			SlipCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Slip", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new() { cost = 0, flippable = true, art = StableSpr.cards_Dodge };

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new AStatus { targetPlayer = true, status = Status.engineStall, statusAmount = 3 },
					new AMove { targetPlayer = true, dir = 1, preferRightWhenZero = true },
					new AMove { targetPlayer = true, dir = 1, preferRightWhenZero = true },
					new AMove { targetPlayer = true, dir = 1, preferRightWhenZero = true },
				],
				Upgrade.A => [
					new AMove { targetPlayer = true, dir = 0, preferRightWhenZero = true },
					new AMove { targetPlayer = true, dir = 0, preferRightWhenZero = true },
				],
				_ => [
					new AMove { targetPlayer = true, dir = 0, preferRightWhenZero = true },
				]
			};
	}

	[UsedImplicitly]
	private sealed class FinalFormCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			FinalFormCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "FinalForm", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new() { cost = 1, art = StableSpr.cards_Overpower };

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new AStatus { targetPlayer = true, status = Status.temporaryCheap, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 9 },
					new AEndTurn(),
				],
				Upgrade.A => [
					new AStatus { targetPlayer = true, status = Status.libra, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 9 },
					new AEndTurn(),
				],
				_ => [
					new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 9 },
					new AEndTurn(),
				]
			};
	}

	[UsedImplicitly]
	private sealed class SkimCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			SkimCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Skim", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new() { cost = 1, art = StableSpr.cards_Options };

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new ADrawCard { count = 5, timer = 1.4 },
					new ADiscard { count = 5 },
				],
				Upgrade.A => [
					new ADrawCard { count = 10, timer = 1.4 },
					new ADiscard(),
				],
				_ => [
					new ADrawCard { count = 5, timer = 1.4 },
					new ADiscard(),
				]
			};
	}

	[UsedImplicitly]
	private sealed class SmashCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			SmashCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Smash", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new() { cost = 1, art = StableSpr.cards_GoatDrone };

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new ASpawn { thing = new Asteroid() },
					new ASpawn { thing = new Asteroid(), offset = -1 },
					new ASpawn { thing = new AttackDrone() },
					new ASpawn { thing = new AttackDrone(), offset = -1 },
				],
				Upgrade.A => [
					new ASpawn { thing = new Asteroid() },
					new ASpawn { thing = new Missile { missileType = MissileType.corrode } },
				],
				_ => [
					new ASpawn { thing = new Asteroid() },
					new ASpawn { thing = new AttackDrone() },
				]
			};
	}

	[UsedImplicitly]
	private sealed class FidgetCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			FidgetCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Fidget", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 0,
				infinite = true,
				flippable = upgrade == Upgrade.A,
				unplayable = upgrade == Upgrade.A && state.route is Combat combat && combat.hand.Count != 0 && (flipped ? combat.hand[0] == this : combat.hand[^1] == this),
				art = StableSpr.cards_ButtonMash,
				description = upgrade == Upgrade.A ? ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fidget", "descriptionA", flipped ? "left" : "right"]) : null,
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.A => [
					new MoveAction { CardId = uuid, Offset = flipped ? -1 : 1 },
				],
				Upgrade.B => [
					new AShuffleHand(),
				],
				_ => []
			};

		private sealed class MoveAction : CardAction
		{
			public required int CardId;
			public required int Offset;

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				var index = c.hand.FindIndex(card => card.uuid == CardId);
				if (index == -1)
				{
					timer = 0;
					return;
				}

				var newIndex = index + Offset;
				if (newIndex < 0 || newIndex >= c.hand.Count)
				{
					timer = 0;
					return;
				}

				var card = c.hand[index];
				c.hand.RemoveAt(index);
				
				if (newIndex == c.hand.Count)
					c.hand.Add(card);
				else
					c.hand.Insert(newIndex, card);
				Audio.Play(Event.CardHandling);
			}
		}
	}
	
	[UsedImplicitly]
	private sealed class ToothExeCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			ToothExeCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.colorless,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Art = StableSpr.cards_colorless,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "ToothExe", "name"]).Localize
			});
		}

		private int GetChoiceCount()
			=> upgrade == Upgrade.B ? 5 : 3;

		public override CardData GetData(State state)
			=> new()
			{
				artTint = DB.decks[Deck.tooth].color.ToString(),
				cost = upgrade == Upgrade.A ? 0 : 1,
				exhaust = true,
				description = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "ToothExe", "description"], new { Count = GetChoiceCount() }),
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> [
				new ToothCardOfferingAction
				{
					amount = GetChoiceCount(),
					limitDeck = Deck.tooth,
					makeAllCardsTemporary = true,
					overrideUpgradeChances = false,
					canSkip = false,
					inCombat = true,
					discount = -1,
					dialogueSelector = ".summonTooth"
				}
			];

		private sealed class ToothCardOfferingAction : ACardOffering
		{
			public override Route? BeginWithRoute(G g, State s, Combat c)
			{
				var changed = new HashSet<string>();
				try
				{
					foreach (var key in AllToothCardKeys)
					{
						if (!DB.cardMetas.TryGetValue(key, out var meta))
							continue;
						if (!meta.dontOffer)
							continue;
						
						changed.Add(key);
						meta.dontOffer = false;
					}

					return base.BeginWithRoute(g, s, c);
				}
				finally
				{
					foreach (var key in changed)
					{
						if (!DB.cardMetas.TryGetValue(key, out var meta))
							continue;
						meta.dontOffer = true;
					}
				}
			}
		}
	}
}
