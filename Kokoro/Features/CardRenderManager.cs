using Microsoft.Xna.Framework;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class CardRenderManager : HookManager<ICardRenderHook>
{
	public Vec ModifyTextCardScale(G g, Card card)
		=> Hooks.Aggregate(Vec.One, (v, hook) => v * hook.ModifyTextCardScale(g, card));

	public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
		=> Hooks.Aggregate(Matrix.Identity, (m, hook) => m * hook.ModifyNonTextCardRenderMatrix(g, card, actions));

	public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
		=> Hooks.Aggregate(Matrix.Identity, (m, hook) => m * hook.ModifyCardActionRenderMatrix(g, card, actions, action, actionWidth));
}