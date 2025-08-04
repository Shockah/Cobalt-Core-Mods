using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class SynchrotronArtifact : Artifact, IRegisterable
{
	internal static IArtifactEntry Entry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("Synchrotron", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Synchrotron.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Synchrotron", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Synchrotron", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> AcceleratedManager.Trait.Configuration.Tooltips?.Invoke(MG.inst.g?.state ?? DB.fakeState, null)?.ToList();

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);

		var cards = state.deck.Concat(combat.hand).Concat(combat.discard)
			.Where(card => card.GetCurrentCost(state) >= 2)
			.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, AcceleratedManager.Trait))
			.ToList();

		if (cards.Count < 3)
			return;

		var card = cards[state.rngActions.NextInt() % cards.Count];
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, AcceleratedManager.Trait, true, false);
		Pulse();
	}
}