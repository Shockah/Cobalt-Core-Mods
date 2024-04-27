using FMOD;
using System;
using System.Collections.Generic;

namespace parchmentArmada.CardActions
{
	internal class ATrinityActivateRandom : CardAction
    {
        public override void Begin(G g, State s, Combat c)
        {
            var parts = s.ship.parts;
            Part[] partList = new Part[3];
            var index = 0;
            var flag = false;
            foreach (Part part in parts)
            {
                if (part.type == PType.cannon)
                {
                    partList[index] = part;
                    if (!part.active) flag = true;
                    index++;
                }
            }
            Random rnd = new Random();
            partList[rnd.Next(1, index)].active = true;
            if (flag)
            {
                flag = false;
                while (!flag)
                {
                    index = rnd.Next(1, 4);
                    if(!partList[index].active)
                    {
                        flag = true;
                        partList[index].active = true;
                    }
                }
            }
            
            Audio.Play(new GUID?(FSPRO.Event.TogglePart));
            //ModManifest.EventHub?.SignalEvent<Combat>("EWanderer.DemoMod.TestEvent", c);
        }

        public override Icon? GetIcon(State s) => new Icon(StableSpr.icons_ace, 42, Colors.attackFail);

        public override List<Tooltip> GetTooltips(State s)
        {
            List<Tooltip> tooltips = new List<Tooltip>();

            //tooltips.Add(new TTGlossary(glossary_item, Array.Empty<object>()));

            return tooltips;
        }
    }
}
