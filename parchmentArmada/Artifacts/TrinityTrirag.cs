using System.Collections.Generic;

namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.Boss }, unremovable = true)]
    internal class TrinityTrirag : Artifact
    {
        public int turns;
        public override void OnCombatStart(State state, Combat combat)
        {
            turns = 0;
        }
        public override void OnTurnStart(State state, Combat combat)
        {
            turns += 1;
            if(turns == 3)
            {
                combat.QueueImmediate(new AAddCard
                {
                    card = new Cards.TrinityRagnarok(),
                    destination = CardDestination.Hand,
                    amount = 1
                });
            }
        }

        public override List<Tooltip>? GetExtraTooltips()
        {
            return new List<Tooltip>
            {
                new TTCard
                {
                    card = new Cards.TrinityRagnarok()
                },
                //new TTGlossary("cardtrait.retain"),
            };
        }
    }
}
