using System;
using System.Collections.Generic;

namespace EvilRiggs.Artifacts;

[ArtifactMeta(pools = [ArtifactPool.Boss])]
internal class HoldThatThought : Artifact
{
	private int count = 0;

	public override void OnCombatStart(State state, Combat combat)
	{
		count = 0;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		if (card.GetData(state).infinite && card.GetData(state).cost > 0)
		{
			count++;
			if (count == 1)
			{
				card.retainOverride = true;
			}
		}
	}

	public override Spr GetSprite()
	{
		if (count < 1)
		{
			return (Spr)Manifest.sprites["artifact_holdThatThought"].Id!.Value;
		}
		return (Spr)Manifest.sprites["artifact_holdThatThoughtUsed"].Id!.Value;
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		List<Tooltip> list = new List<Tooltip>();
		list.Add((Tooltip)new TTGlossary("cardtrait.infinite", Array.Empty<object>()));
		list.Add((Tooltip)new TTGlossary("cardtrait.retain", Array.Empty<object>()));
		return list;
	}
}
