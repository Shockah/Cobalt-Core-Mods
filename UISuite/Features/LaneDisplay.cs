using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
		public LaneDisplayStyle InactiveDisplayStyle = LaneDisplayStyle.SoftStriped;
		
		[JsonProperty]
		public LaneDisplayStyle ActiveDisplayStyle = LaneDisplayStyle.SoftStriped;
		
		[JsonProperty]
		public double InactiveAlpha = 0.05;
		
		[JsonProperty]
		public double ActiveAlpha = 0.15;
		
		[JsonProperty]
		public double InactiveSpeed = 0.5;
		
		[JsonProperty]
		public double ActiveSpeed = 3;

		[JsonConverter(typeof(StringEnumConverter))]
		public enum LaneDisplayStyle
		{
			SoftStriped, HardStriped, SolidGray, SolidWhite
		}
	}
}

internal sealed class LaneDisplay : IRegisterable
{
	private static readonly MGColor[] LaneDividerSoftStripe = [
		MGColor.White, MGColor.White, MGColor.White, MGColor.White, MGColor.White, MGColor.White,
		MGColor.LightGray, MGColor.Gray, MGColor.DarkGray,
		MGColor.Black, MGColor.Black, MGColor.Black, MGColor.Black, MGColor.Black, MGColor.Black,
		MGColor.DarkGray, MGColor.Gray, MGColor.LightGray,
	];
	private static readonly MGColor[] LaneDividerHardStripe = [
		MGColor.White, MGColor.White, MGColor.White, MGColor.White, MGColor.White, MGColor.White,
		MGColor.Black, MGColor.Black, MGColor.Black, MGColor.Black, MGColor.Black, MGColor.Black,
	];
	private static readonly MGColor[] LaneDividerSolidGray = [MGColor.Gray];
	private static readonly MGColor[] LaneDividerSolidWhite = [MGColor.White];
	
	private static ISpriteEntry LaneDividerInactiveSprite = null!;
	private static ISpriteEntry LaneDividerActiveSprite = null!;
	
	private static bool IsActiveHover;
	private static double LaneDividerYOffset;
	private static Texture2D? LaneDividerInactiveTexture;
	private static Texture2D? LaneDividerActiveTexture;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		LaneDividerInactiveSprite = helper.Content.Sprites.RegisterDynamicSprite("LaneDividerInactive", () => ObtainLaneDividerTexture(false));
		LaneDividerActiveSprite = helper.Content.Sprites.RegisterDynamicSprite("LaneDividerActive", () => ObtainLaneDividerTexture(true));
		
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
					api.MakeEnumStepper(
						title: () => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveDisplayStyle", "Title"]),
						getter: () => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveDisplayStyle,
						setter: value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveDisplayStyle = value
					).SetValueFormatter(
						value => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveDisplayStyle", "Value", value.ToString()])
					).SetValueWidth(
						_ => 90
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.ActiveDisplayStyle)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveDisplayStyle", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveDisplayStyle", "Description"])
						}
					]),
					api.MakeEnumStepper(
						title: () => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveDisplayStyle", "Title"]),
						getter: () => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveDisplayStyle,
						setter: value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveDisplayStyle = value
					).SetValueFormatter(
						value => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveDisplayStyle", "Value", value.ToString()])
					).SetValueWidth(
						_ => 90
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.InactiveDisplayStyle)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveDisplayStyle", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveDisplayStyle", "Description"])
						}
					]),
					api.MakeNumericStepper(
						() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveAlpha", "Title"]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveAlpha,
						value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveAlpha = Math.Round(value, 2),
						minValue: 0,
						maxValue: 1,
						step: 0.01
					).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
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
						value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveAlpha = Math.Round(value, 2),
						minValue: 0,
						maxValue: 1,
						step: 0.01
					).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
						new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.InactiveAlpha)}")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveAlpha", "Title"]),
							Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveAlpha", "Description"]),
						},
					]),
					api.MakeConditional(
						api.MakeNumericStepper(
							() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveSpeed", "Title"]),
							() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveSpeed,
							value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveSpeed = Math.Round(value, 2),
							minValue: -10,
							maxValue: 10,
							step: 0.25
						).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
							new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.ActiveSpeed)}")
							{
								TitleColor = Colors.textBold,
								Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveSpeed", "Title"]),
								Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "ActiveSpeed", "Description"]),
							},
						]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveDisplayStyle is ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.SoftStriped or ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.HardStriped
					),
					api.MakeConditional(
						api.MakeNumericStepper(
							() => ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveSpeed", "Title"]),
							() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveSpeed,
							value => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveSpeed = Math.Round(value, 2),
							minValue: -10,
							maxValue: 10,
							step: 0.25
						).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
							new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LaneDisplay)}::{nameof(ProfileSettings.LaneDisplay.InactiveSpeed)}")
							{
								TitleColor = Colors.textBold,
								Title = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveSpeed", "Title"]),
								Description = ModEntry.Instance.Localizations.Localize(["LaneDisplay", "Settings", "InactiveSpeed", "Description"]),
							},
						]),
						() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveDisplayStyle is ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.SoftStriped or ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.HardStriped
					)
				]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled
			),
		]);

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		LaneDividerActiveTexture?.Dispose();
		LaneDividerActiveTexture = null;
		LaneDividerInactiveTexture?.Dispose();
		LaneDividerInactiveTexture = null;
	}

	private static Texture2D ObtainLaneDividerTexture(bool active)
	{
		if ((active ? LaneDividerActiveTexture : LaneDividerInactiveTexture) is { } texture)
			return texture;

		var stripe = GetLaneDividerStripe(active);
		texture = new Texture2D(MG.inst.graphics.GraphicsDevice, 1, MG.inst.PIX_H + stripe.Length);
		
		if (active)
			LaneDividerActiveTexture = texture;
		else
			LaneDividerInactiveTexture = texture;

		var data = new MGColor[texture.Width * texture.Height];
		for (var y = 0; y < texture.Height; y++)
			data[y] = stripe[y % stripe.Length];
		
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

	private static MGColor[] GetLaneDividerStripe(bool active)
		=> (active ? ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveDisplayStyle : ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveDisplayStyle) switch
		{
			ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.SoftStriped => LaneDividerSoftStripe,
			ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.HardStriped => LaneDividerHardStripe,
			ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.SolidGray => LaneDividerSolidGray,
			ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.SolidWhite => LaneDividerSolidWhite,
			_ => throw new ArgumentOutOfRangeException()
		};

	private static void Combat_RenderShipsOver_Postfix_Low(Combat __instance, G g)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.IsEnabled)
		{
			IsActiveHover = false;
			return;
		}

		var stripe = ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveDisplayStyle == ProfileSettings.LaneDisplaySettings.LaneDisplayStyle.HardStriped ? LaneDividerHardStripe : LaneDividerSoftStripe;
		var sprite = IsActiveHover ? LaneDividerActiveSprite : LaneDividerInactiveSprite;

		if (stripe.Length > 1)
		{
			var speed = IsActiveHover
				? ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.ActiveSpeed
				: ModEntry.Instance.Settings.ProfileBased.Current.LaneDisplay.InactiveSpeed;
			LaneDividerYOffset += speed * stripe.Length * g.dt;
			LaneDividerYOffset = Math.Abs(LaneDividerYOffset) % stripe.Length * Math.Sign(LaneDividerYOffset);
		}
		
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
			Draw.Sprite(sprite.Sprite, currentLaneX - 3, LaneDividerYOffset - stripe.Length, color: Colors.white.fadeAlpha(alpha));
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