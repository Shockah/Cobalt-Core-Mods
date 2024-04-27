using System.Collections.Generic;

namespace EvilRiggs.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.Boss })]
	internal class HoldThatThought : Artifact
	{
		int count = 0;

		public override void OnCombatStart(State state, Combat combat)
		{
			count = 0;
		}

		public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
		{
			if(card.GetData(state).infinite && card.GetData(state).cost > 0)
			{
				count++;
				if(count==1) {
					card.retainOverride = true;
				}
			}
		}

		public override Spr GetSprite()
		{
			if (count < 1)
			{
				return (Spr)Manifest.sprites["artifact_holdThatThought"].Id!;
			}

			return (Spr)Manifest.sprites["artifact_holdThatThoughtUsed"].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("cardtrait.infinite"));
			list.Add(new TTGlossary("cardtrait.retain"));
			return list;
		}
	}
}
