using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		ICardTraitEntry SpontaneousTriggeredTrait { get; }
		bool TryGetSpontanenousAction(CardAction maybeSpontanenousAction, out CardAction? action);
		CardAction MakeSpontaneousAction(CardAction action);
	}
}
