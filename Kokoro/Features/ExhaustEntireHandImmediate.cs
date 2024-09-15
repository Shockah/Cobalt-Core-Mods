using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public CardAction MakeExhaustEntireHandImmediate()
			=> new AExhaustEntireHandImmediate();
	}
}

public sealed class AExhaustEntireHandImmediate : AExhaustEntireHand
{
	public override void Begin(G g, State s, Combat c)
	{
		timer = 0.0;
		foreach (var item in ((IEnumerable<Card>)c.hand).Reverse())
			c.QueueImmediate(new AExhaustOtherCard { uuid = item.uuid });
	}
}
