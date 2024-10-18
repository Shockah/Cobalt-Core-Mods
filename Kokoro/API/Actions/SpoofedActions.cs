using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		bool TryGetSpoofedAction(CardAction maybeSpoofedAction, [MaybeNullWhen(false)] out CardAction renderAction, [MaybeNullWhen(false)] out CardAction realAction);
		CardAction MakeSpoofed(CardAction renderAction, CardAction realAction);
	}
}