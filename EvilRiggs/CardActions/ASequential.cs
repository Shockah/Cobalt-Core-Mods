using System;
using System.Collections.Generic;
using CobaltCoreModding.Definitions.ExternalItems;

namespace EvilRiggs.CardActions;

internal class ASequential : CardAction
{
	public Card? targetCard;

	public override void Begin(G g, State s, Combat c)
	{
		base.timer = 0.0;
		if (targetCard != null)
		{
			targetCard.flipped = true;
		}
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = new List<Tooltip>();
		ExternalGlossary obj = Manifest.glossary["sequentialHint"];
		TTGlossary glossary = new TTGlossary(obj.Head, Array.Empty<object>());
		tooltips.Add((Tooltip)(object)glossary);
		return tooltips;
	}
}
