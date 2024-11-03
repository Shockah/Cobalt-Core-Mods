using Microsoft.Xna.Framework;
using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class CardScalingManager : IKokoroApi.IV2.ICardRenderingApi.IHook
{
	public CardScalingManager()
	{
		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(this);
	}

	public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
		=> (card as IDraculaCard)?.ModifyNonTextCardRenderMatrix(g, actions) ?? Matrix.Identity;

	public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
		=> (card as IDraculaCard)?.ModifyCardActionRenderMatrix(g, actions, action, actionWidth) ?? Matrix.Identity;
}
