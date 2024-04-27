using System.Collections.Generic;

namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.Boss }, unremovable = true)]
    internal class ErisDroneHubV2 : Artifact
    {
        public override void OnReceiveArtifact(State s)
        {
            foreach (Artifact artifact in s.artifacts)
            {
                if (artifact is Artifacts.ErisDroneHub)
                {
                    artifact.OnRemoveArtifact(s);
                }
            }

            s.artifacts.RemoveAll((Artifact r) => r is Artifacts.ErisDroneHub);
        }
        public override void OnCombatStart(State state, Combat combat)
        {
            combat.QueueImmediate(new AAddCard
            {
                card = new Cards.ErisStrifeForgeCard(),
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
                    card = new Cards.ErisStrifeForgeCard()
                },
                //new TTGlossary("cardtrait.retain"),
            };
        }
    }
}
