using FMOD;

namespace parchmentArmada.Artifacts
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    internal class TrinityTrivenge : Artifact
    {
        public override string Description()
        {
            return "Your cannons are inactive. When a cannon is hit by an attack, activate it until the end of the turn.";
        }


        public override string Name()
        {
            return "TRI-VENGE";
        }

        public override void OnCombatEnd(State state)
        {
            this.DeactivateAllCannons(state);
        }

        public override void OnTurnEnd(State state, Combat combat)
        {
            this.DeactivateAllCannons(state);
        }

        private void DeactivateAllCannons(State state)
        {
            bool flag = false;
            foreach (Part part in state.ship.parts)
            {
                if (part.type == PType.cannon && part.active)
                {
                    flag = true;
                    
                }
                if (part.type == PType.cannon)
                {
                    part.active = false;
                }
            }
            if (!flag) return;
            Audio.Play(new GUID?(FSPRO.Event.TogglePart));
        }
    }
}
