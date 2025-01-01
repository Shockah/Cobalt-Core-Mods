using System;
using System.Collections.Generic;

namespace Shockah.MORE;

internal class ModifiedEnemy : AI
{
	public required AI AI;
	public double HullPercentage = 1;
	public Dictionary<Status, int> Statuses = [];
	
	public override Ship BuildShipForSelf(State s)
	{
		var ai = Mutil.DeepCopy(AI);
		var ship = ai.BuildShipForSelf(s);
		character = ai.character;
		ModifyShip(s, ref ship);
		return ship;
	}

	public override void OnCombatStart(State s, Combat c)
	{
		var ai = Mutil.DeepCopy(AI);
		c.otherShip.ai = ai;
		
		var ship = ai.BuildShipForSelf(s);
		ModifyShip(s, ref ship);
		c.otherShip = ship;
		c.otherShip.ai = ai;
		
		ai.OnCombatStart(s, c);
	}

	protected virtual void ModifyShip(State s, ref Ship ship)
	{
		if (Math.Abs(HullPercentage - 1) > 0.01)
		{
			ship.hullMax = (int)Math.Ceiling(ship.hullMax * HullPercentage);
			ship.hull = Math.Min((int)Math.Ceiling(ship.hull * HullPercentage), ship.hullMax);
		}

		foreach (var (status, amount) in Statuses)
			ship.Add(status, amount);
	}
}