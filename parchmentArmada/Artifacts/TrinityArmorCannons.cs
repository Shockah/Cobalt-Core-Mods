namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.Boss }, unremovable = true)]   
    internal class TrinityArmorCannons : Artifact
    {
        public override void OnReceiveArtifact(State state)
        {
            foreach (Part part in state.ship.parts)
            {
                if (part.type == PType.cannon)
                {
                    part.damageModifier = PDamMod.armor;
                }
            }
            state.ship.shieldMaxBase -= 2;
        }
    }
}
