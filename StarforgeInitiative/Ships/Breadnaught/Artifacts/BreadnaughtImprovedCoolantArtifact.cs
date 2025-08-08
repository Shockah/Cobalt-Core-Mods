using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtImprovedCoolantArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BreadnaughtImprovedCoolant", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common],
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Breadnaught/Artifact/ImprovedCoolant.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "ImprovedCoolant", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "ImprovedCoolant", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(BreadnaughtBarrelSpin.BarrelSpinStatus.Status, 1);

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (state.ship.Get(BreadnaughtBarrelSpin.BarrelSpinStatus.Status) <= 0)
			state.ship.Set(BreadnaughtBarrelSpin.BarrelSpinStatus.Status);
	}
}