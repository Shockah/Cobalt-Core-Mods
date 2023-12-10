using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class AttackApologyCard : ApologyCard
{
	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Cannon;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AAttack
			{
				damage = GetDmg(s, 1)
			}
		};
}