namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		bool TryGetOnTurnEndAction(CardAction maybeOnTurnEndAction, out CardAction? action);
		CardAction MakeOnTurnEndAction(CardAction action);
	}
}
