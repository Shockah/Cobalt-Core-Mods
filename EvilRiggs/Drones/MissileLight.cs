using System;
using System.Collections.Generic;
using CobaltCoreModding.Definitions.ExternalItems;

namespace EvilRiggs.Drones;

internal class MissileLight : Missile
{
	public override void Render(G g, Vec v)
	{
		Color exhaustColor = new("fff387");
		Vec offset = ((StuffBase)this).GetOffset(g, true);
		Vec val = new Vec(Math.Sin((double)((StuffBase)this).x + g.state.time * 10.0), Math.Cos((double)((StuffBase)this).x + g.state.time * 20.0 + Math.PI / 2.0));
		Vec vec = val.round();
		offset += vec;
		int num = 0;
		int num2 = num;
		Vec vec2 = v + offset;
		bool flag = ((StuffBase)this).targetPlayer;
		bool flag2 = false;
		Spr spr = (Spr)Manifest.sprites["missile_light"].Id!.Value;
		Vec vec3 = default(Vec);
		if (num2 < 0)
		{
			vec3 += new Vec(-6.0, (double)(((StuffBase)this).targetPlayer ? 4 : (-4)));
		}
		if (num2 > 0)
		{
			vec3 += new Vec(6.0, (double)(((StuffBase)this).targetPlayer ? 4 : (-4)));
		}
		if (!((StuffBase)this).targetPlayer)
		{
			vec3 += new Vec(0.0, 21.0);
		}
		Vec vec4 = vec2 + vec3 + new Vec(7.0, 8.0);
		double num3 = vec4.x - 5.0;
		double y = vec4.y + (double)((!((StuffBase)this).targetPlayer) ? 14 : 0);
		Vec? originRel = new Vec(0.0, 1.0);
		bool flag3 = !((StuffBase)this).targetPlayer;
		bool flipX = flag2;
		bool flipY = flag3;
		Color? color = exhaustColor;
		Spr id2 = spr;
		flag3 = flag;
		((StuffBase)this).DrawWithHilight(g, id2, vec2, flag2, flag3);
		Vec val2 = vec4 + new Vec(0.5, -2.5);
		Color val3 = exhaustColor;
		Color val4 = new Color(1.0, 0.5, 0.5, 1.0);
		Glow.Draw(val2, 25.0, val3 * val4.gain(0.2 + 0.1 * Math.Sin(g.state.time * 30.0 + (double)((StuffBase)this).x) * 0.5));
	}

	public override List<CardAction>? GetActions(State s, Combat c)
	{
		return new List<CardAction> { (CardAction)new AMissileHit
		{
			worldX = ((StuffBase)this).x,
			outgoingDamage = 1,
			targetPlayer = ((StuffBase)this).targetPlayer
		} };
	}

	public override List<Tooltip> GetTooltips()
	{
		List<Tooltip> list = new List<Tooltip>();
		ExternalGlossary obj = Manifest.glossary["missileLight"];
		list.Add((Tooltip)new TTGlossary(obj.Head, new object[1] { "<c=damage>1</c>" })
		{
			flipIconY = false
		});
		if (((StuffBase)this).bubbleShield)
		{
			list.Add((Tooltip)new TTGlossary("midrow.bubbleShield", Array.Empty<object>()));
		}
		return list;
	}

	public override Spr? GetIcon()
	{
		return (Spr)Manifest.sprites["icon_missile_light"].Id!.Value;
	}
}
