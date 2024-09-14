using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class AExhaustEntireHandImmediate : AExhaustEntireHand
{
	public override void Begin(G g, State s, Combat c)
	{
		timer = 0.0;
		foreach (var item in ((IEnumerable<Card>)c.hand).Reverse())
			c.QueueImmediate(new AExhaustOtherCard { uuid = item.uuid });
	}
}
