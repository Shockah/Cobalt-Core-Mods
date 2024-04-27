using System;
using System.Collections.Generic;

namespace EvilRiggs.Drones
{
	internal class MissileLight : Missile
	{
		public override void Render(G g, Vec v)
		{
			Color exhaustColor = new Color("fff387");
			Vec offset = GetOffset(g, doRound: true);
			Vec vec = new Vec(Math.Sin((double)x + g.state.time * 10.0), Math.Cos((double)x + g.state.time * 20.0 + Math.PI / 2.0)).round();
			offset += vec;
			int num;

			num = 0;
			int num2 = num;
			Vec vec2 = v + offset;
			bool flag = targetPlayer;
			bool flag2 = false;
			Spr spr = (Spr)Manifest.sprites["missile_light"].Id!;

			Vec vec3 = default(Vec);
			if (num2 < 0)
			{
				vec3 += new Vec(-6.0, targetPlayer ? 4 : (-4));
			}

			if (num2 > 0)
			{
				vec3 += new Vec(6.0, targetPlayer ? 4 : (-4));
			}

			if (!targetPlayer)
			{
				vec3 += new Vec(0.0, 21.0);
			}

			Vec vec4 = vec2 + vec3 + new Vec(7.0, 8.0);
			bool flag4;
			double num3 = vec4.x - 5.0;
			double y = vec4.y + (double)((!targetPlayer) ? 14 : 0);
			Vec? originRel = new Vec(0.0, 1.0);
			flag4 = !targetPlayer;
			bool flipX = flag2;
			bool flipY = flag4;
			Color? color = exhaustColor;
			Spr id2 = spr;
			flag4 = flag;
			DrawWithHilight(g, id2, vec2, flag2, flag4);
			Glow.Draw(vec4 + new Vec(0.5, -2.5), 25.0, exhaustColor * new Color(1.0, 0.5, 0.5).gain(0.2 + 0.1 * Math.Sin(g.state.time * 30.0 + (double)x) * 0.5));
		}

		public override List<CardAction>? GetActions(State s, Combat c)
		{
			return new List<CardAction>
			{
				new AMissileHit
				{
					worldX = x,
					outgoingDamage = 1,
					targetPlayer = targetPlayer
				}
			};
		}

		public override List<Tooltip> GetTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary(Manifest.glossary["missileLight"].Head, "<c=damage>1</c>")
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
			return (Spr)Manifest.sprites["icon_missile_light"].Id!;
		}
	}
}
