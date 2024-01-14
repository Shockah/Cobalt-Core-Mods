namespace Shockah.Shared;

internal static class CardActionExt
{
	public static CardAction Disabled(this CardAction self, bool disabled = true)
	{
		self.disabled = disabled;
		return self;
	}

	public static CardAction CanRunAfterKill(this CardAction self, bool canRunAfterKill = true)
	{
		self.canRunAfterKill = canRunAfterKill;
		return self;
	}
}