using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class FlickerFleetingCoreArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("FlickerFleetingCore", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Flicker/Artifact/FleetingCore.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "artifact", "FleetingCore", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "artifact", "FleetingCore", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.energyLessNextTurn, 1),
			.. StatusMeta.GetTooltips(FlickerBorrow.AfterflickStatus.Status, 1),
		];

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);

		var borrowed = state.ship.Get(Status.energyLessNextTurn) > 0;
		var hasAfterflick = state.ship.Get(FlickerBorrow.AfterflickStatus.Status) > 0;
		if (borrowed == hasAfterflick)
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = FlickerBorrow.AfterflickStatus.Status, statusAmount = borrowed ? 1 : 0 });
	}
}