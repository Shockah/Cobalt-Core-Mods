using daisyowl.text;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	void RegisterCardRenderHook(ICardRenderHook hook, double priority);
	void UnregisterCardRenderHook(ICardRenderHook hook);

	Font PinchCompactFont { get; }
}

public interface ICardRenderHook
{
	bool ShouldDisableCardRenderingTransformations(G g, Card card) => false;
	Font? ReplaceTextCardFont(G g, Card card) => null;
	Vec ModifyTextCardScale(G g, Card card) => Vec.One;
	Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions) => Matrix.Identity;
	Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth) => Matrix.Identity;
}