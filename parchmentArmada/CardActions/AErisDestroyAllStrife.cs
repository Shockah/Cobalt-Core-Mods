using parchmentArmada.Ships;
using System.Collections.Generic;

namespace parchmentArmada.CardActions
{
	internal class AErisDestroyAllStrife : CardAction
    {
        public override void Begin(G g, State s, Combat c)
        {
            foreach (StuffBase midrow in c.stuff.Values)
            {
                if(midrow is Drones.ErisStrifeEngine)
                {
                    //Audio.Play(Event.Hits_DroneCollision);
                    midrow.DoDestroyedEffect(s, c);
                    Drones.ErisStrifeEngine mid2 = (Drones.ErisStrifeEngine)midrow;
                    mid2.charge = 0;
                    mid2.eCharge = 0;
                    //c.stuff.Remove(midrow.x);
                }
            }
        }

        public override Icon? GetIcon(State s) => new Icon((Spr)Eris.sprites["eris_anerisMini"].Id!, null, Colors.status);

        public override List<Tooltip> GetTooltips(State s)
        {
            List<Tooltip> tooltips = new List<Tooltip>();
            TTGlossary glossary;
            glossary = new TTGlossary(Eris.ErisAnerisGlossary.Head);
            tooltips.Add(glossary);

            return tooltips;
        }
    }
}
