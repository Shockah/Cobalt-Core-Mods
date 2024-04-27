namespace CobaltPetrichor.CardActions
{
	internal class AUkulele : CardAction
	{
		public int target;
		public override void Begin(G g, State s, Combat c)
		{
			int num = target + s.ship.x;
			RaycastResult raycastResult = CombatUtils.RaycastGlobal(c, c.otherShip, fromDrone: true, num);
			timer = 0.2;
			DamageDone dmg = new DamageDone
			{
				hitHull = false,
				hitShield = true,
				poppedShield = false
			};
			
			if (raycastResult.hitShip) {
				c.QueueImmediate(new AStunPart { worldX = num, timer = 0 });
				c.QueueImmediate(new AHurt { targetPlayer = false, hurtAmount = 1, timer = 0, hurtShieldsFirst=true });
			}
			EffectSpawner.Cannon(g, false, raycastResult, dmg, true);
			EffectSpawner.Cannon(g, false, raycastResult, dmg, true);
		}
	}
}
