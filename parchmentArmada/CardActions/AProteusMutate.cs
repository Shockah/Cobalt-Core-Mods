using FSPRO;
using parchmentArmada.Ships;
using System.Collections.Generic;

namespace parchmentArmada.CardActions
{
	internal class AProteusMutate : CardAction
    {
        public int pos;
        public PType type;
        public string skin = null!;
        public override void Begin(G g, State s, Combat c)
        {
            var parts = s.ship.parts;
            if (pos != 0)
            {
                Part part = parts[pos - 1];
                part.skin = "@mod_part:parchment.armada.proteus." + skin;
                part.type = type;
                part.damageModifier = PDamMod.none;
                if (skin == "armor" ) { part.damageModifier = PDamMod.armor; }
                if (skin == "cannon2") { part.damageModifier = PDamMod.weak; }
                if (skin == "missiles2") { part.damageModifier = PDamMod.weak; }
                if (skin == "reactor") { part.damageModifier = PDamMod.brittle; }
                if (skin == "cockpit")
                {
                    if (s.GetDifficulty() >= 3) { part.damageModifier = PDamMod.brittle; }
                    else if (s.GetDifficulty() >= 1) { part.damageModifier = PDamMod.weak; }
                }
                
                Audio.Play(Event.TogglePart);
            }
        }

        public override Icon? GetIcon(State s)
        {
            return new Icon((Spr)Proteus.sprites["proteus_target"+pos].Id!, null, Colors.textMain);
        }

        public override List<Tooltip> GetTooltips(State s)
        {
            List<Tooltip> tooltips = new List<Tooltip>();
            TTGlossary glossary;
            glossary = new TTGlossary(Proteus.glossary["target"+pos].Head);
            tooltips.Add(glossary);

            return tooltips;
        }
    }
}
