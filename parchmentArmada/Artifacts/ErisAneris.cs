using System.Collections.Generic;

namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.Boss }, unremovable = true)]
    internal class ErisAneris : Artifact
    {
        public override void OnCombatStart(State state, Combat combat)
        {
            combat.QueueImmediate(new AAddCard
            {
                card = new Cards.ErisAnerisCard(),
                destination = CardDestination.Hand,
                amount = 1
            });
        }

        public override List<Tooltip>? GetExtraTooltips()
        {
            return new List<Tooltip>
            {
                new TTCard
                {
                    card = new Cards.ErisAnerisCard()
                },
                //new TTGlossary("cardtrait.retain"),
            };
        }
    }
}
