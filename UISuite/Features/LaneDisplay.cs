using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.UISuite;

internal sealed class LaneDisplay : IRegisterable
{
	private static bool IsActiveHover;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderShipsOver)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderShipsOver_Postfix_Low)), priority: Priority.Low)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.BubbleEvents)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_BubbleEvents_Postfix))
		);
	}

	private static bool IsActiveLaneDisplayAction(CardAction action)
	{
		if (action is AMove)
			return true;
		if (action is ADroneMove)
			return true;
		if (action is ASpawn spawnAction && spawnAction.offset != 0)
			return true;
		
		return false;
	}

	private static bool IsActiveLaneUiBox(Box box)
	{
		if (box.key == StableUK.btn_move_left || box.key == StableUK.btn_move_right)
			return true;
		if (box.key == StableUK.btn_moveDrones_left || box.key == StableUK.btn_moveDrones_right)
			return true;
		
		return false;
	}

	private static void Combat_RenderShipsOver_Postfix_Low(Combat __instance, G g)
	{
		const int laneSpacing = 16;

		var camX = __instance.GetCamOffset().x;
		if (camX < 0)
			camX = g.mg.PIX_W - (-camX % g.mg.PIX_W);

		var firstLaneOnScreenX = Math.Round(camX) % laneSpacing;
		var currentLaneX = firstLaneOnScreenX;

		while (currentLaneX < g.mg.PIX_W)
		{
			Draw.Rect(currentLaneX - 3, 0, 1, g.mg.PIX_H, Colors.black.fadeAlpha(IsActiveHover ? 0.2 : 0.05));
			currentLaneX += laneSpacing;
		}

		IsActiveHover = false;
	}

	private static void Card_Render_Postfix(Card __instance, G g, int? renderAutopilot)
	{
		if (IsActiveHover)
			return;
		
		var cardUiKey = __instance.UIKey();
		if (g.boxes.FirstOrDefault(b => b.key == cardUiKey) is not { } box)
			return;
		if (box.onMouseDown != g.state.route)
			return;
		if (!box.IsHover())
			return;

		if (renderAutopilot.HasValue && renderAutopilot.Value != 0)
		{
			IsActiveHover = true;
			return;
		}
			
		if (!__instance.GetActionsOverridden(g.state, g.state.route as Combat ?? DB.fakeCombat).Any(IsActiveLaneDisplayAction))
			return;
		
		IsActiveHover = true;
	}

	private static void G_BubbleEvents_Postfix(G __instance)
	{
		if (IsActiveHover)
			return;
		if (__instance.hoverKey is not { } hoverKey)
			return;
		if (__instance.boxes.FirstOrDefault(b => b.key == hoverKey) is not { } box)
			return;
		if (!IsActiveLaneUiBox(box))
			return;
		
		IsActiveHover = true;
	}
}