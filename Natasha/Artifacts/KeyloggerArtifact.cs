using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class KeyloggerArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Keylogger", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Keylogger.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Keylogger", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Keylogger", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [.. (ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [])];

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.energy <= 0)
			return;
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is KeyloggerArtifact) is not { } artifact)
			return;
		if (!state.deck.Concat(combat.discard).Concat(combat.hand).Any(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.KokoroApi.Limited.Trait)))
			return;

		combat.QueueImmediate(new Action { artifactPulse = artifact.Key() });
	}

	private sealed class Action : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			if (HandleCards(c.hand))
				return;
			if (HandleCards(s.deck))
				return;
			HandleCards(c.discard);

			bool HandleCards(IEnumerable<Card> cards)
			{
				var cardList = cards.Where(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.KokoroApi.Limited.Trait)).ToList();
				switch (cardList.Count)
				{
					case 0:
						return false;
					case 1:
						HandleCard(cardList[0]);
						return true;
					default:
						HandleCard(cardList[s.rngActions.NextInt() % cardList.Count]);
						return true;
				}

			}

			void HandleCard(Card card)
			{
				ModEntry.Instance.KokoroApi.Limited.SetLimitedUses(s, card, ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, card) + 1);
				Audio.Play(Event.Status_PowerUp);
			}
		}
	}
}