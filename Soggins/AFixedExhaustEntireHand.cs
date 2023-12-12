using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

public sealed class AFixedExhaustEntireHand : AExhaustEntireHand
{
	public override void Begin(G g, State s, Combat c)
	{
		timer = 0.0;
		foreach (Card item in ((IEnumerable<Card>)c.hand).Reverse())
			c.QueueImmediate(new AExhaustOtherCard
			{
				uuid = item.uuid
			});
	}
}
