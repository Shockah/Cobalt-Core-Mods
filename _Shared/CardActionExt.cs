namespace Shockah.Shared;

internal static class CardActionExt
{
	public static CardAction Disabled(this CardAction self, bool disabled = true)
	{
		self.disabled = disabled;
		return self;
	}

	public static CardAction OmitFromTooltips(this CardAction self, bool value = true)
	{
		self.omitFromTooltips = value;
		return self;
	}

	public static CardAction CanRunAfterKill(this CardAction self, bool canRunAfterKill = true)
	{
		self.canRunAfterKill = canRunAfterKill;
		return self;
	}

	public static void FullyRun(this CardAction self, G g, State state, Combat combat)
	{
		self.Begin(g, state, combat);

		if (self.timer > 0)
		{
			var oldDt = g.dt;
			g.dt = 1000;

			while (self.timer > 0)
				self.Update(g, state, combat);

			g.dt = oldDt;
		}
	}
}