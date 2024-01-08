using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class SpacingCardRenderHook : ICardRenderHook
{
	private Card? LastCard { get; set; }

	public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
	{
		LastCard = card;
		if (card is not IDraculaCard modCard || modCard.ActionRenderingSpacing == 1)
			return Matrix.Identity;
		return Matrix.CreateScale(1f, 1f, 1f);
	}

	public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
	{
		if ((card ?? LastCard) is not IDraculaCard modCard || modCard.ActionRenderingSpacing == 1)
			return Matrix.Identity;
		return Matrix.CreateScale(1f, 1f, 1f);
	}
}
