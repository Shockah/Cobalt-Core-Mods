using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Natasha;

internal sealed class NatashaRiggsArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("NatashaRiggs", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Riggs.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "NatashaRiggs", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "NatashaRiggs", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.riggs]);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null)?.ToList();

	public override void OnPlayerDeckShuffle(State state, Combat combat)
	{
		base.OnPlayerDeckShuffle(state, combat);

		if (HandlePile(combat.hand))
			return;
		if (HandlePile(state.deck))
			return;
		if (HandlePile(combat.discard))
			return;
		HandlePile(combat.exhausted);

		bool HandlePile(IEnumerable<Card> cards)
		{
			var limitedCards = cards.Where(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.KokoroApi.Limited.Trait)).ToList();
			
			switch (limitedCards.Count)
			{
				case 1:
				{
					var action = ModEntry.Instance.KokoroApi.Limited.MakeChangeLimitedUsesAction(limitedCards[0].uuid, 1).AsCardAction;
					action.artifactPulse = Key();
					combat.QueueImmediate(action);
					return true;
				}
				case >= 2:
				{
					var action = ModEntry.Instance.KokoroApi.Limited.MakeChangeLimitedUsesAction(limitedCards[state.rngActions.NextInt() % limitedCards.Count].uuid, 1).AsCardAction;
					action.artifactPulse = Key();
					combat.QueueImmediate(action);
					return true;
				}
				default:
					return false;
			}
		}
	}
}