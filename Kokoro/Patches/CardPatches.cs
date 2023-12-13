using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal static class CardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Card_GetAllTooltips_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(typeof(CardPatches), nameof(Card_RenderAction_Prefix))
		);
	}

	private static void Card_GetAllTooltips_Postfix(ref IEnumerable<Tooltip> __result)
	{
		__result = Instance.WormStatusManager.ModifyCardTooltips(__result);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AConditional conditional)
			return true;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		conditional.Expression?.Render(g, ref position, action.disabled, dontDraw);

		if (!dontDraw)
			Draw.Sprite((Spr)Instance.Content.QuestionMarkSprite.Id!.Value, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += SpriteLoader.Get((Spr)Instance.Content.QuestionMarkSprite.Id!.Value)?.Width ?? 0;
		position.x += 1;

		if (conditional.Action is { } wrappedAction)
		{
			g.Push(rect: new(position.x - initialX, 0));
			position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();
		}

		__result = (int)position.x - initialX;
		g.Pop();
		return false;
	}
}
