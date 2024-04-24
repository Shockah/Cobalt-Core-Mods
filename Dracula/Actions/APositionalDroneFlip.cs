﻿using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

public sealed class APositionalDroneFlip : CardAction
{
	[JsonProperty]
	public required int WorldX;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (!c.stuff.TryGetValue(WorldX, out var @object))
		{
			timer = 0;
			return;
		}
		timer *= 0.5;

		@object.targetPlayer = !@object.targetPlayer;
		for (var i = 0; i < 50; i++)
			PFX.combatExplosionWhiteSmoke.Add(new Particle
			{
				pos = new Vec(@object.x * 16, 60.0) + new Vec(7.5, 4.0) + Mutil.RandVel(),
				vel = Mutil.RandVel() * 30.0,
				lifetime = Mutil.NextRand() * 0.6,
				size = 3.0 + Mutil.NextRand() * 8.0
			});
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		if (s.route is Combat combat && combat.stuff.TryGetValue(WorldX, out var @object))
			@object.hilight = 2;

		return [
			new GlossaryTooltip("action.droneFlipSingle")
			{
				Icon = StableSpr.icons_droneFlip,
				TitleColor = Colors.action,
				Title = Loc.T("action.droneFlipSingle.name"),
				Description = Loc.T("action.droneFlipSingle.desc")
			}
		];
	}

	public override Icon? GetIcon(State s)
		=> new(StableSpr.icons_droneFlip, null, Colors.textMain);
}
