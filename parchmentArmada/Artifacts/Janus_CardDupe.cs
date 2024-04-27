using System;

namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    internal class Janus_CardDupe : Artifact
    {
        public int plays;
        public override void OnCombatStart(State state, Combat combat) { plays = 0; }
        public override void OnTurnStart(State state, Combat combat) { plays = 0; }
        public override void OnCombatEnd(State state) { plays = 0; }

        public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
        {
            if (plays <= 1)
            {
                Card newCard = card.CopyWithNewId();
                newCard.discount = -newCard.GetData(state).cost;
                newCard.temporaryOverride = true;
                newCard.singleUseOverride = true;
                combat.Queue(new AAddCard { card = newCard, destination = CardDestination.Hand, artifactPulse = Key() });
            }
            plays++;
        }

        public override int? GetDisplayNumber(State s)
        {
            return Math.Max(2-plays, 0);
        }
    }
}
