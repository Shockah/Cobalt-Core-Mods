using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class AttackApologyCard : ApologyCard
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AAttack
			{
				damage = GetDmg(s, 1)
			}
		};
}