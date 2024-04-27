using System.Collections.Generic;

namespace EvilRiggs.CardActions;

internal class ADiscountFirstCards : CardAction
{
	public int amount;

	public int offset;

	public override void Begin(G g, State s, Combat c)
	{
		if (c.hand[offset] != null)
		{
			Card obj = c.hand[offset];
			obj.discount--;
			if (amount > 1)
			{
				c.QueueImmediate((CardAction)(object)new ADiscountFirstCards
				{
					amount = amount - 1,
					offset = offset + 1
				});
			}
		}
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		return new List<Tooltip>();
	}
}
