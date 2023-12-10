using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class ShieldApologyCard : ApologyCard
{
	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Shield;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = Status.shield,
				statusAmount = 1,
				targetPlayer = true
			}
		};
}