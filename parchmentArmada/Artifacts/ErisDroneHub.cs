using System.Collections.Generic;

namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    internal class ErisDroneHub : Artifact
    {
        public int turnCounter;

        public override void OnCombatStart(State state, Combat combat)
        {
            turnCounter = 1;
            if (turnCounter == 1)
            {
                combat.QueueImmediate(new AAddCard
                {
                    card = new Cards.ErisStrifeEngineCard(),
                    destination = CardDestination.Hand,
                    amount = 1
                });
            }
        }
        public override void OnTurnStart(State state, Combat combat)
        {
            /*if(turnCounter == 1) { 
                combat.QueueImmediate(new AAddCard
                {
                    card = new Cards.ErisStrifeEngineCard(),
                    destination = CardDestination.Hand,
                    amount = 1
                });
            }*/
            turnCounter += 1;
        }

        /*public override void OnReceiveArtifact(State state)
        {
            foreach (Card card in state.deck)
            {
                if (card.Name() == new CannonColorless().Name() )
                {
                    state.deck.Remove(card);
                    break;
                }
            }
            state.deck.Add(new DroneshiftColorless());
        }*/

        public override List<Tooltip>? GetExtraTooltips()
        {
            return new List<Tooltip>
            {
                new TTCard
                {
                    card = new Cards.ErisStrifeEngineCard()
                },
                //new TTGlossary("cardtrait.retain"),
            };
        }
    }
}
