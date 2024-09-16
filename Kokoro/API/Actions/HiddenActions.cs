namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		CardAction MakeHidden(CardAction action, bool showTooltips = false);
	}
}