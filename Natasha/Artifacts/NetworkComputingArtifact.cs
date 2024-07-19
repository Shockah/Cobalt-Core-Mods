using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class NetworkComputingArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("NetworkComputing", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/NetworkComputing.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "NetworkComputing", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "NetworkComputing", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [.. (Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [])];

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Limited.Trait))
			return;

		combat.Queue(new AZeroDoublerAction
		{
			uuid = card.uuid,
			backupCard = card.CopyWithNewId(),
			artifactPulse = Key(),
		});
	}
}