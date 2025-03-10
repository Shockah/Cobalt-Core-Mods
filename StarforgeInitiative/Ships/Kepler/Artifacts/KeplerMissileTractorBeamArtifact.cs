using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerMissileTractorBeamArtifact : Artifact, IRegisterable, IKeplerMissileHitHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("KeplerMissileTractorBeam", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Kepler/Artifact/MissileTractorBeam.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "MissileTractorBeam", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "MissileTractorBeam", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = new KeplerRelaunchCard() }];

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.GetCurrentQueue().QueueImmediate(new ALoseArtifact { artifactType = new KeplerMissilePDSArtifact().Key() });
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

		var copy = Mutil.DeepCopy(missile);
		copy.isHitting = false;
		copy.targetPlayer = false;
		combat.QueueImmediate(new AAddCard { destination = CardDestination.Hand, card = new KeplerRelaunchCard { Thing = copy } });
		
		Pulse();
		@continue = false;
		return false;
	}
}