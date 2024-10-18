using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		bool TryGetHiddenAction(CardAction maybeSpontanenousAction, [MaybeNullWhen(false)] out CardAction action);
		CardAction MakeHidden(CardAction action, bool showTooltips = false);
	}
}