namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public partial interface IActionApi;
}