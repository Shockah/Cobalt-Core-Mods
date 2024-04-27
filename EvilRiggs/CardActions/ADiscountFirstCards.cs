using System.Collections.Generic;

namespace EvilRiggs.CardActions
{
	internal class ADiscountFirstCards : CardAction
	{
		public int amount;
		public int offset;
		public override void Begin(G g, State s, Combat c)
		{
			if(c.hand[offset] != null)
			{
				c.hand[offset].discount -= 1;
				if (amount >  1)
				{
					c.QueueImmediate(new ADiscountFirstCards { amount = amount-1, offset = offset+1 });
				}
			}
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			List<Tooltip> tooltips = new List<Tooltip>();
			//TTGlossary glossary;
			//glossary = new TTGlossary(Manifest.glossary["sequential"]?.Head);
			//tooltips.Add(glossary);

			return tooltips;
		}
	}
}
