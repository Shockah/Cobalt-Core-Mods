using FMOD;
using FSPRO;

namespace Shockah.Shared;

internal static class EffectSpawnerExt
{
	public static void HitEffect(G g, bool targetPlayer, RaycastResult ray, DamageDone dmg)
	{
		if (g.state.route is not Combat combat)
			return;

		if (ray.hitShip)
		{
			var rectVecA = ray.fromDrone
				? FxPositions.DroneCannon(ray.worldX, targetPlayer)
				: FxPositions.Cannon(ray.worldX, !targetPlayer);

			Vec rectVecB;
			if (ray is { hitDrone: false, hitShip: false })
				rectVecB = FxPositions.Miss(ray.worldX, targetPlayer);
			else if (ray.hitDrone)
				rectVecB = FxPositions.Drone(ray.worldX);
			else if (dmg.hitHull)
				rectVecB = FxPositions.Hull(ray.worldX, targetPlayer);
			else
				rectVecB = FxPositions.Shield(ray.worldX, targetPlayer);

			var rect = Rect.FromPoints(rectVecA, rectVecB);
			var hitPos = new Vec(rect.x, targetPlayer ? rect.y2 : rect.y);
			ParticleBursts.HullImpact(g, hitPos, targetPlayer, !ray.hitDrone, ray.fromDrone);
		}

		if (dmg is { hitShield: true, hitHull: false })
		{
			combat.fx.Add(new ShieldHit { pos = FxPositions.Shield(ray.worldX, targetPlayer) });
			ParticleBursts.ShieldImpact(g, FxPositions.Shield(ray.worldX, targetPlayer), targetPlayer);
		}

		if (dmg.poppedShield)
			combat.fx.Add(new ShieldPop { pos = FxPositions.Shield(ray.worldX, targetPlayer) });

		GUID? sound = null;
		if (dmg.poppedShield)
			sound = Event.Hits_ShieldPop;
		else if (dmg.hitShield)
			sound = Event.Hits_ShieldHit;

		if (ray is { hitDrone: false, hitShip: false })
			sound = Event.Hits_Miss;
		else if (dmg.hitHull)
			sound = targetPlayer ? Event.Hits_HitHurt : Event.Hits_OutgoingHit;
		else if (ray.hitDrone)
			sound = Event.Hits_HitDrone;

		if (sound.HasValue)
			Audio.Play(sound.Value);
	}
}