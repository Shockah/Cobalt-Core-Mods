namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    internal class HeliosArtifact : Artifact
    {
        public override void OnCombatEnd(State state)
        {
            var parts = state.ship.parts;
            foreach (Part part in parts)
            {
                if ((part.type == PType.cannon) || (part.type == PType.special))
                {
					if (part.skin?.EndsWith(".helios.cannon") == true) part.type = PType.cannon;
                    else part.type = PType.special;
                }
            }
        }

        public override string Name()
        {
            return "MINIATURE STAR";
        }

        public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
        {
            base.OnQueueEmptyDuringPlayerTurn(state, combat);
        }
    }
}
