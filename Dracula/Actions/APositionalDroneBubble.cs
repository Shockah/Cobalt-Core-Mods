using Newtonsoft.Json;
using System.Collections.Generic;

namespace Shockah.Dracula;

public sealed class APositionalDroneBubble : CardAction
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

		@object.bubbleShield = true;
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
			new TTGlossary("midrow.bubbleShield")
		];
	}

	public override Icon? GetIcon(State s)
		=> new(StableSpr.icons_bubbleShield, null, Colors.textMain);
}
