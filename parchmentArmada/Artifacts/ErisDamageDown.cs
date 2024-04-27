namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    internal class ErisDamageDown : Artifact
    {
        public override int ModifyBaseDamage(int baseDamage, Card? card, State state, Combat? combat, bool fromPlayer)
        {
            if (!fromPlayer)
            {
                return 0;
            }

            return -1;
        }
    }
}
