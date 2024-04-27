using FMOD;
using System.Collections.Generic;

namespace parchmentArmada.CardActions
{
	internal class AHeliosFireLaser : CardAction
    {
        public override void Begin(G g, State s, Combat c)
        {
            var parts = s.ship.parts;
            foreach (Part part in parts)
            {
                if (part.type == PType.cannon) part.type = PType.special;
                else if (part.type == PType.special) part.type = PType.cannon;
            }
            timer = 0;
            /*int num = s.ship.parts.FindIndex((Part p) => p.type == PType.cannon && p.active) + s.ship.x;
            if (num != -1)
            {
                Rect rect = Rect.FromPoints(FxPositions.Cannon(num, true), FxPositions.Miss(num, false));
                c.fx.Add(new FXHeliosLaser
                {
                    rect = rect
                });
            }*/
            
            Audio.Play(new GUID?(FSPRO.Event.Plink));
        }
        public override Icon? GetIcon(State s) => new Icon(StableSpr.icons_ace, null, Colors.status);

        public override List<Tooltip> GetTooltips(State s)
        {
            List<Tooltip> tooltips = new List<Tooltip>();

            //tooltips.Add(new TTGlossary(glossary_item, Array.Empty<object>()));

            return tooltips;
        }
    }
}
