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
	{
		var scale = (card as IDraculaCard)?.TextScaling ?? 1f;
		return new(scale, scale);
	}

	public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
	{
		var scale = (card as IDraculaCard)?.ActionSpacingScaling ?? 1f;
		return scale == 1f ? Matrix.Identity : Matrix.CreateScale(scale);
	}

	public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
	{
		var scale = (card as IDraculaCard)?.ActionSpacingScaling ?? 1f;
		return scale == 1f ? Matrix.Identity : Matrix.CreateScale(1f / scale);
	}
}
