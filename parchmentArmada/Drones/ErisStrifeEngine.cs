using System;
using System.Collections.Generic;

namespace parchmentArmada.Drones
{
	internal class ErisStrifeEngine : StuffBase
    {
        public bool upgraded = false;
        public int charge = 0;
        public int eCharge = 0;
        private double time = 0;
        public double fakeOffset = 0;

        public override bool Invincible()
        {
            return true;
        }

        public override bool IsHostile()
        {
            return targetPlayer;
        }

        public override bool IsFriendly()
        {
            return !targetPlayer;
        }

        private void drawCircles(Vec v, double xSize, double ySize, double xPos, double yPos, int amt, bool useSprite, bool foreground)
        {
            for (int i = 0; i < amt; i++)
            {
                var deg = ((360 / amt) * i) * (Math.PI / 180);
                double offset = useSprite ? (20 * (Math.PI / 180)) : 0;
                offset += yPos > 15.0 ? 20 : 0;
                var x = xSize * Math.Cos(time + deg + offset) + v.x + xPos;
                var y = ySize * Math.Sin(time + deg + offset) + v.y + yPos;
                if(foreground && Math.Sin(time + deg + offset) > 0)
                {
                    if(useSprite) Draw.Sprite((Spr)Ships.Eris.sprites["eris_sparkle"].Id!, x, y + fakeOffset, false, false);
                    if(!useSprite) Draw.Rect(x, y + fakeOffset, 1, 1, new Color(0, 0, 0));
                }
                if (!foreground && Math.Sin(time + deg + offset) <= 0)
                {
                    if (useSprite) Draw.Sprite((Spr)Ships.Eris.sprites["eris_sparkle"].Id!, x, y + fakeOffset, false, false);
                    if (!useSprite) Draw.Rect(x, y + fakeOffset, 1, 1, new Color(0, 0, 0));
                }
            }
        }

        public override void Render(G g, Vec v)
        {
            drawCircles(v + GetOffset(g), 8.0, 2.0, 5.0, 5.5, charge, true, false);
            //drawCircles(v, 8.0, 2.0, 7.0, 6.5, charge, false, false);
            drawCircles(v + GetOffset(g), 8.0, 2.0, 5.0, 24.5, eCharge, true, false);
            //drawCircles(v, 8.0, 2.0, 7.0, 25.5, eCharge, false, false);
            DrawWithHilight(g, (Spr)Ships.Eris.sprites["eris_strifeEngine"].Id!, v + GetOffset(g) + new Vec(0,fakeOffset), flipX: false, targetPlayer);
            //Draw.Rect(v.x - 1.0, v.y - 1.0, 5.0, (charge*2));
            /*for (int i=0; i < charge; i++)
            {
                var deg = ((360 / charge) * i) * (Math.PI/180);
                var x = 8.0 * Math.Cos(time + deg) + v.x + 5.0;
                var y = 2.0 * Math.Sin(time + deg) + v.y + 4.5;
                Draw.Sprite((Spr)Ships.Eris.sprites["eris_sparkle"].Id, x, y, false, true);
                var x2 = 8.0 * Math.Cos(time + deg + (-20 * (Math.PI / 180))) + v.x + 7.0;
                var y2 = 2.0 * Math.Sin(time + deg + (-20 * (Math.PI / 180))) + v.y + 6.5;
                Draw.Rect(x2, y2, 1, 1, new Color(0, 0, 0));
            }
            for (int i = 0; i < eCharge; i++)
            {
                var deg = ((360 / eCharge) * i) * (Math.PI / 180);
                var x = 8.0 * Math.Cos(time + deg) + v.x + 5.0;
                var y = 2.0 * Math.Sin(time + deg) + v.y + 24.5;
                Draw.Sprite((Spr)Ships.Eris.sprites["eris_sparkle"].Id, x, y, false, true);
                var x2 = 8.0 * Math.Cos(time + deg + (-20 * (Math.PI / 180))) + v.x + 7.0;
                var y2 = 2.0 * Math.Sin(time + deg + (-20 * (Math.PI / 180))) + v.y + 26.5;
                Draw.Rect(x2, y2, 1, 1, new Color(0, 0, 0));
            }*/
            drawCircles(v + GetOffset(g), 8.0, 2.0, 5.0, 5.5, charge, true, true);
            //drawCircles(v, 8.0, 2.0, 7.0, 6.5, charge, false, true);
            drawCircles(v + GetOffset(g), 8.0, 2.0, 5.0, 24.5, eCharge, true, true);
            //drawCircles(v, 8.0, 2.0, 7.0, 25.5, eCharge, false, true);
            time += 0.04;
            fakeOffset = fakeOffset * 0.92;
            if(Math.Abs(fakeOffset) < 0.25) { fakeOffset = 0; }
        }

        public override List<CardAction>? GetActionsOnBonkedWhileInvincible(State s, Combat c, bool wasPlayer, StuffBase thing)
        {
            if (wasPlayer) { charge += 1; if (charge > 5) charge = 5; }
            if (!wasPlayer) { eCharge += 2; if (eCharge > 5) eCharge = 5; }
            return null;
        }

        private int AttackDamage()
        {
            return charge;
        }

        public override List<CardAction>? GetActions(State s, Combat c)
        {
            List<CardAction>? list = new List<CardAction> { };
            if(charge>0)
            {
                
                for (int i = 0; i < charge - 1; i++)
                {
                    list.Add(new CardActions.AErisFireStrife { drone = this, start = true, targetPlayer = false });
                    list.Add(new AAttack { fromDroneX = x, targetPlayer = false, damage = 1, fast = true });
                }
                list.Add(new CardActions.AErisFireStrife { drone = this, start = true, targetPlayer = false });
                list.Add(new CardActions.AErisFireStrife { drone = this, start = false, targetPlayer = false });
                list.Add(new AAttack { fromDroneX = x, targetPlayer = false, damage = 1 });
            }
            if (eCharge > 0)
            {
                
                for (int i = 0; i < eCharge - 1; i++)
                {
                    list.Add(new CardActions.AErisFireStrife { drone = this, start = true, targetPlayer = true });
                    list.Add(new AAttack { fromDroneX = x, targetPlayer = true, damage = 1, fast = true });
                }
                list.Add(new CardActions.AErisFireStrife { drone = this, start = true, targetPlayer = true });
                list.Add(new CardActions.AErisFireStrife { drone = this, start = false, targetPlayer = true });
                list.Add(new AAttack { fromDroneX = x, targetPlayer = true, damage = 1 });
            }
            return list;
        }

        public override List<CardAction>? GetActionsOnShotWhileInvincible(State s, Combat c, bool wasPlayer, int damage)
        {
            //charge += damage+1;
            //if (charge > 5) charge = 5;
            //if(wasPlayer) { targetPlayer = false; } else { targetPlayer = true; }
            if (wasPlayer)
            {
                charge += damage + 1;
                if (charge > 5) charge = 5;
            }
            else
            {
                eCharge += damage + 1;
                if (eCharge > 5) eCharge = 5;
            }
            return new List<CardAction> { };
        }

        public override List<Tooltip> GetTooltips()
        {
            List<Tooltip> list = new List<Tooltip>();
            list.Add(new TTGlossary(Ships.Eris.ErisStrifeEngineGlossary.Head, "<c=damage>" + charge + "</c>", "<c=damage>" + eCharge + "</c>")
            {
                flipIconY = false
            });

            if (bubbleShield)
            {
                list.Add(new TTGlossary("midrow.bubbleShield"));
            }

            return list;
        }

        public override Spr? GetIcon()
        {
            return (Spr)Ships.Eris.sprites["eris_strifeMini"].Id!;
        }
    }
}
