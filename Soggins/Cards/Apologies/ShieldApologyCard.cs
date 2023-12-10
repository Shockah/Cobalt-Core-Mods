using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class ShieldApologyCard : ApologyCard
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0
		};

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