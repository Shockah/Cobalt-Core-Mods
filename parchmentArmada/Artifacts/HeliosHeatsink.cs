namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    internal class HeliosHeatsink : Artifact
    {
        public override void OnTurnStart(State state, Combat combat)
        {
            if (state.ship.Get(Status.serenity) == 0)
            {
                combat.Queue(new AStatus { status = Status.serenity, statusAmount = 1, targetPlayer = true, artifactPulse = this.Key() });
            }
        }
    }
}
