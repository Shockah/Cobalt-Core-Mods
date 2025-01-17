using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nickel;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.UISuite;

internal sealed class LaneDisplay : IRegisterable
{
	private static ISpriteEntry LaneDividerSprite = null!;
	
	private static bool IsActiveHover;
	private static Texture2D? LaneDividerTexture;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		LaneDividerSprite = helper.Content.Sprites.RegisterDynamicSprite("LaneDivider", ObtainLaneDividerTexture);
		
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

	private static Texture2D ObtainLaneDividerTexture()
	{
		if (LaneDividerTexture is { } texture)
			return texture;

		texture = new Texture2D(MG.inst.graphics.GraphicsDevice, 1, MG.inst.PIX_H);
		LaneDividerTexture = texture;

		var data = new MGColor[texture.Width * texture.Height];
		for (var y = 0; y < texture.Height; y++)
			data[y] = (y / 6) % 2 == 0 ? MGColor.White : MGColor.Black;
		
		texture.SetData(data);
		return texture;
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
			Draw.Sprite(LaneDividerSprite.Sprite, currentLaneX - 3, 0, color: Colors.white.fadeAlpha(IsActiveHover ? 0.15 : 0.05));
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