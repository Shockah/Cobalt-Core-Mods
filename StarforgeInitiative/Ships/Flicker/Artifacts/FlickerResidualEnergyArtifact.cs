using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class FlickerResidualEnergyArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("FlickerResidualEnergy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common],
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Flicker/Artifact/ResidualEnergy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "artifact", "ResidualEnergy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "artifact", "ResidualEnergy", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.energyLessNextTurn, 1),
			.. StatusMeta.GetTooltips(ModEntry.Instance.KokoroApi.StatusNextTurn.TempShield, 1),
		];

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (state.ship.Get(Status.energyLessNextTurn) <= 0)
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = true, status = ModEntry.Instance.KokoroApi.StatusNextTurn.TempShield, statusAmount = 1, artifactPulse = Key() });
	}
}