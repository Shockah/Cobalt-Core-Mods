using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class CardScalingManager : ICardRenderHook
{
	public CardScalingManager()
	{
		ModEntry.Instance.KokoroApi.RegisterCardRenderHook(this, 0);
	}

	public Vec ModifyTextCardScale(G g, Card card)
		=> (card as IDraculaCard)?.ModifyTextCardScale(g) ?? Vec.One;

	public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
		=> (card as IDraculaCard)?.ModifyNonTextCardRenderMatrix(g, actions) ?? Matrix.Identity;

	public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
		=> (card as IDraculaCard)?.ModifyCardActionRenderMatrix(g, actions, action, actionWidth) ?? Matrix.Identity;

	//public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
	//{
	//	if (card is BatFormCard && card.upgrade != Upgrade.B)
	//	{
	//		int spacing = 48;
	//		int newXOffset = 48;
	//		int newYOffset = 40;
	//		var index = actions.IndexOf(action);
	//		return index switch
	//		{
	//			0 => Matrix.CreateTranslation(-newXOffset, -newYOffset - (int)((index - 1.5) * spacing), 0),
	//			1 => Matrix.CreateTranslation(newXOffset, -newYOffset - (int)((index - 1.5) * spacing), 0),
	//			2 => Matrix.CreateTranslation(newXOffset, newYOffset - (int)((index - 1.5) * spacing), 0),
	//			3 => Matrix.CreateTranslation(-newXOffset, newYOffset - (int)((index - 1.5) * spacing), 0),
	//			_ => Matrix.Identity
	//		};
	//	}

	//	var scale = (card as IDraculaCard)?.ActionSpacingScaling ?? 1f;
	//	return scale == 1f ? Matrix.Identity : Matrix.CreateScale(1f / scale);
	//}
}
