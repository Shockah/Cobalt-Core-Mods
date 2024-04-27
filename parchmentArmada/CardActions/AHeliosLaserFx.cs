namespace parchmentArmada.CardActions
{
	internal class AHeliosLaserFx : CardAction
    {
        public override void Begin(G g, State s, Combat c)
        {
            int num = s.ship.parts.FindIndex((Part p) => p.type == PType.cannon && p.active) + s.ship.x;
            if (num != -1)
            {
                Rect rect = Rect.FromPoints(FxPositions.Cannon(num, true), FxPositions.Miss(num, false));
                c.fx.Add(new FXHeliosLaser
                {
                    rect = rect
                });
            }
            var parts = s.ship.parts;
            foreach (Part part in parts)
            {
                if (part.type == PType.cannon) part.pulse = 1.0;
            }
            timer = 0;
        }
    }
}
