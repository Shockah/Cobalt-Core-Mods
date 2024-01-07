namespace Shockah.Shared;

internal static class CardActionExt
{
	public static CardAction Disabled(this CardAction self, bool disabled = true)
	{
		self.disabled = disabled;
		return self;
	}
}