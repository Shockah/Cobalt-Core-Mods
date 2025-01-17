using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public LaneDisplaySettings LaneDisplay = new();

	internal sealed class LaneDisplaySettings
	{
		[JsonProperty]
		public bool IsEnabled = true;
		
		[JsonProperty]
		public double InactiveAlpha = 0.05;
		
		[JsonProperty]
		public double ActiveAlpha = 0.15;
		
		[JsonProperty]
		public double InactiveSpeed = 0.5;
		
		[JsonProperty]
		public double ActiveSpeed = 3;
	}
}

internal sealed class LaneDisplay : IRegisterable
{
	private const int LaneDividerStripLength = 6;
	
	private static ISpriteEntry LaneDividerSprite = null!;
	
	private static bool IsActiveHover;
	private static double LaneDividerYOffset;
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
	
	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "IsEnabled", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.IsEnabled)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "IsEnabled", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "IsEnabled", "Description"]),
				},
			]),
			api.MakeConditional(
				api.MakeList([
					api.MakeNumericStepper(
						() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveAlpha", "Title"]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveAlpha,
						value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveAlpha = value,
						minValue: 0,
						maxValue: 1,
						step: 0.01
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.ActiveAlpha)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveAlpha", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveAlpha", "Description"]),
						},
					]),
					api.MakeNumericStepper(
						() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveAlpha", "Title"]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveAlpha,
						value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveAlpha = value,
						minValue: 0,
						maxValue: 1,
						step: 0.01
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.InactiveAlpha)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveAlpha", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveAlpha", "Description"]),
						},
					]),
					api.MakeNumericStepper(
						() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveSpeed", "Title"]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveSpeed,
						value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveSpeed = value,
						minValue: -10,
						maxValue: 10,
						step: 0.25
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.ActiveSpeed)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveSpeed", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveSpeed", "Description"]),
						},
					]),
					api.MakeNumericStepper(
						() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveSpeed", "Title"]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveSpeed,
						value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveSpeed = value,
						minValue: -10,
						maxValue: 10,
						step: 0.25
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.InactiveSpeed)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveSpeed", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveSpeed", "Description"]),
						},
					])
				]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled
			),
		]);

	private static Texture2D ObtainLaneDividerTexture()
	{
		if (LaneDividerTexture is { } texture)
			return texture;

		texture = new Texture2D(MG.inst.graphics.GraphicsDevice, 1, MG.inst.PIX_H + LaneDividerStripLength);
		LaneDividerTexture = texture;

		var data = new MGColor[texture.Width * texture.Height];
		for (var y = 0; y < texture.Height; y++)
			data[y] = (y / LaneDividerStripLength) % 2 == 0 ? MGColor.White : MGColor.Black;
		
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
		if (!ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled)
		{
			IsActiveHover = false;
			return;
		}

		var speed = IsActiveHover
			? ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveSpeed
			: ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveSpeed;
		LaneDividerYOffset += speed * LaneDividerStripLength * 2 * g.dt;
		LaneDividerYOffset = Math.Abs(LaneDividerYOffset) % (LaneDividerStripLength * 2) * Math.Sign(LaneDividerYOffset);
		
		const int laneSpacing = 16;

		var camX = __instance.GetCamOffset().x;
		if (camX < 0)
			camX = g.mg.PIX_W - (-camX % g.mg.PIX_W);

		var firstLaneOnScreenX = Math.Round(camX) % laneSpacing;
		var currentLaneX = firstLaneOnScreenX;
		
		var alpha = IsActiveHover
			? ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveAlpha
			: ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveAlpha;

		while (currentLaneX < g.mg.PIX_W)
		{
			Draw.Sprite(LaneDividerSprite.Sprite, currentLaneX - 3, LaneDividerYOffset - LaneDividerStripLength * 2, color: Colors.white.fadeAlpha(alpha));
			currentLaneX += laneSpacing;
		}

		IsActiveHover = false;
	}

	private static void Card_Render_Postfix(Card __instance, G g, int? renderAutopilot)
	{
		if (IsActiveHover)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled)
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
		if (!ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled)
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