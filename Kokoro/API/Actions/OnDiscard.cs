namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		bool TryGetOnDiscardAction(CardAction maybeOnDiscardAction, out CardAction? action);
		CardAction MakeOnDiscardAction(CardAction action);
	}
}
