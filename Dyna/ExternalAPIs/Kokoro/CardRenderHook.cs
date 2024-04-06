using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.Dyna;

public partial interface IKokoroApi
{
	void RegisterCardRenderHook(ICardRenderHook hook, double priority);
	void UnregisterCardRenderHook(ICardRenderHook hook);
}

public interface ICardRenderHook
{
	Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions) => Matrix.Identity;
	Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth) => Matrix.Identity;
}