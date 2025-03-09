using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerMissilePDSArtifact : Artifact, IRegisterable, IKeplerMissileHitHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("KeplerMissilePDS", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Kepler/Artifact/MissilePDS.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "MissilePDS", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "MissilePDS", "description"]).Localize
		});
	}

	public bool OnMissileHit(State state, Combat combat, Ship ship, Missile missile, AMissileHit action, RaycastResult ray, ref bool @continue, ref int damage)
	{
		if (!ship.isPlayerShip)
			return false;
		if (ship.GetPartAtWorldX(ray.worldX) is not { } part)
			return false;
		if (part.type != PType.missiles)
			return false;
		if (part.active)
			return false;

		Pulse();
		@continue = false;
		return false;
	}
}