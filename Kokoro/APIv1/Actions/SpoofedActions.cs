namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		CardAction MakeSpoofed(CardAction renderAction, CardAction realAction);
	}
}