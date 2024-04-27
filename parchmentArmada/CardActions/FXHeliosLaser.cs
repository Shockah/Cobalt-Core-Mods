using System;

namespace parchmentArmada.CardActions
{
	internal class FXHeliosLaser : FX
    {
        public Rect rect;

        public static Color cannonBeam = new Color("ff8866");

        public static Color cannonBeamCore = new Color("ffffff");

        public static Color black = new Color("000000");

        new public bool IsDone => age > 2.0;

        public double calcSize(double i) => 4.0 * (1.0 - Math.Pow(2.8, (-8 * i))) - 1;

        public override void Render(G g, Vec v)
        {
            double num = 0.8;
            if (age < num)
            {
                //double num2 = 7.8 * (1.0 - age / num);
                //double num2 = 4.0 * (1.0 - Math.Pow(2.8, (-8 * (1.0 - age/num)))) - 1;
                double num2 = Math.Min(calcSize(1.0 - age / num), calcSize(age/num));
                double offsetX = 5.5;
                double offsetY = 1.2;
                double xPos = v.x + rect.x - num2 + offsetX;
                double yPos = v.y + rect.y + offsetY;
                double wPos = rect.w + num2 * 2.0 + offsetX;
                double hPos = rect.h + offsetY;
                Draw.Rect(xPos - 1.5, yPos, wPos + 3.0, hPos, cannonBeam, BlendMode.Screen);
                if (num2 > 0)
                {
                    Draw.Rect(xPos, yPos, wPos, hPos, cannonBeamCore);
                }
                /*double xPos2 = v.x + rect.x + offsetX;
                for (double i=1.0; i<=6.0; i++)
                {
                    double ii = Math.Pow(6.0-i,2) / 20;
                    Draw.Rect(xPos2 + (i * (0 - num2 / 4.0)), yPos, 1, hPos - ii, cannonBeamCore);
                    Draw.Rect(xPos2 - (i * (0 - num2 / 4.0)), yPos, 1, hPos - ii, cannonBeamCore);
                }*/
                int count = 0;
                foreach (Part part in g.state.ship.parts)
                {
                    if (part.type == PType.cannon) count += 1;
                }
                foreach (Part part in g.state.ship.parts)
                {
                    if (part.type == PType.cannon && count > 1) part.pulse = 1.0;
                    if (part.type == PType.special && count <= 1) part.pulse = 1.0;
                }
            }
        }
    }
}
