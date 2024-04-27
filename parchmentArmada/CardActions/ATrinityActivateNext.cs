using FMOD;
using parchmentArmada.Ships;
using System;
using System.Collections.Generic;

namespace parchmentArmada.CardActions
{
	internal class ATrinityActivateNext : CardAction
    {
        public int amount;
        public override void Begin(G g, State s, Combat c)
        {
            var parts = s.ship.parts;
            var flag = false;
            foreach (Part part in parts)
            {
                if (part.type == PType.cannon && !part.active)
                {
                    Audio.Play(new GUID?(FSPRO.Event.TogglePart));
                    part.active = true;
                    flag = true;
                    amount -= 1;
                    break;
                }
            }

            if(amount > 0 && flag)
            {
                c.QueueImmediate(new ATrinityActivateNext() { amount = amount });
            }

            
            //ModManifest.EventHub?.SignalEvent<Combat>("EWanderer.DemoMod.TestEvent", c);
        }

        public override Icon? GetIcon(State s) => new Icon((Spr)Trinity.sprites["trinity_mini"].Id!, amount, Colors.status);

        public override List<Tooltip> GetTooltips(State s)
        {
            List<Tooltip> tooltips = new List<Tooltip>();
            TTGlossary glossary;
            if(amount <= 1) {
                glossary = new TTGlossary(Trinity.TrinityActivateNextGlossary?.Head ?? throw new Exception("Missing Trinity Glossary"), "", "");
            }
            else
            {
                glossary = new TTGlossary(Trinity.TrinityActivateNextGlossary?.Head ?? throw new Exception("Missing Trinity Glossary"), amount + " ", "s");
            }
            tooltips.Add(glossary);

            return tooltips;
        }
    }
}
