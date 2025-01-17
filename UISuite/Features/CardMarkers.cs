using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public CardMarkerSettings CardMarkers = new();

	internal sealed class CardMarkerSettings
	{
		[JsonProperty]
		public bool IsEnabled = true;
	}
}

file static class CardBrowseExt
{
	public static CardMarkers.MarkerType? GetSelectedMarkerType(this CardBrowse route)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<CardMarkers.MarkerType>(route, "SelectedMarkerType");
	
	public static void SetSelectedMarkerType(this CardBrowse route, CardMarkers.MarkerType? markerType)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "SelectedMarkerType", markerType);

	public static int GetSelectedColorIndex(this CardBrowse route)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(route, "SelectedColorIndex");
	
	public static void SetSelectedColorIndex(this CardBrowse route, int colorIndex)
		=> ModEntry.Instance.Helper.ModData.SetModData(route, "SelectedColorIndex", colorIndex);

	public static bool IsClearMarkersSelected(this CardBrowse route)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(route, "IsClearMarkersSelected");

	public static void ToggleClearMarkersSelected(this CardBrowse route)
		=> ModEntry.Instance.Helper.ModData.SetModData(route, "IsClearMarkersSelected", !route.IsClearMarkersSelected());
}

file static class CardExt
{
	public static List<CardMarkers.Marker>? GetMarkers(this Card card)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<List<CardMarkers.Marker>>(card, "Markers");

	public static void ToggleMarker(this Card card, CardMarkers.MarkerType markerType, Color color)
	{
		var marker = new CardMarkers.Marker(markerType, color.ToString());
		
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<List<CardMarkers.Marker>>(card, "Markers") is { } markers)
		{
			var index = markers.IndexOf(marker);
			if (index == -1)
				markers.Add(marker);
			else
				markers.RemoveAt(index);
			
			while (markers.Count > CardMarkers.MaxMarkers)
				markers.RemoveAt(0);
		}
		else
		{
			ModEntry.Instance.Helper.ModData.SetOptionalModData<List<CardMarkers.Marker>>(card, "Markers", [marker]);
		}
	}

	public static void ClearMarkers(this Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "Markers");
}

internal sealed class CardMarkers : IRegisterable
{
	[JsonConverter(typeof(StringEnumConverter))]
	internal enum MarkerType
	{
		[UsedImplicitly] Cross,
		[UsedImplicitly] Rhombus,
		[UsedImplicitly] RotatedSquare,
		[UsedImplicitly] Square,
		[UsedImplicitly] Star,
		[UsedImplicitly] Triangle
	}

	internal record Marker(
		MarkerType Type,
		string ColorHex
	);

	internal const int MaxMarkers = 3;
	private static readonly UK CardMarkerTypeUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK CardMarkerColorUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK ClearCardMarkersUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	private static readonly Color[] MarkerColors = [
		new("FFFFFF"),
		new("3F3F3F"),
		new("FF0000"),
		new("FF7F00"),
		new("FFFF00"),
		new("00FF00"),
		new("00FFFF"),
		new("0000FF"),
		new("FF00FF"),
	];

	private static Dictionary<MarkerType, ISpriteEntry> MarkerIcons = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		MarkerIcons = Enum.GetValues<MarkerType>()
			.Select(v => (v, ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Marker/{Enum.GetName(v)}.png"))))
			.ToDictionary();
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.OnMouseDown)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_OnMouseDown_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDeck)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDeck_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDiscard_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderExhaust)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderExhaust_Postfix))
		);
	}
	
	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["CardMarkers", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["CardMarkers", "Settings", "IsEnabled", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.CardMarkers)}::{nameof(ProfileSettings.CardMarkers.IsEnabled)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["CardMarkers", "Settings", "IsEnabled", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardMarkers", "Settings", "IsEnabled", "Description"]),
				},
			]),
			api.MakeConditional(
				api.MakeList([]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled
			),
		]);

	private static void RenderMarkers(List<Marker> markers, Vec position, Vec spacing)
	{
		for (var i = 0; i < markers.Count; i++)
		{
			var marker = markers[i];
			var color = new Color(marker.ColorHex);
			Draw.Sprite(MarkerIcons[marker.Type].Sprite, position.x + i * spacing.x, position.y + i * spacing.y, color: color);
		}
	}

	private static void RenderMarkerOverlayIfNeeded(G g, UIKey uiKey, List<Card> cards)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled)
			return;
		if (g.boxes.FirstOrDefault(b => b.key == uiKey) is not { } box)
			return;

		var allMarkers = cards
			.SelectMany(card => card.GetMarkers() ?? [])
			.Distinct()
			.Select(m => (Marker: m, Color: new Color(m.ColorHex)))
			.OrderBy(e => e.Marker.Type)
			.ThenBy(e => e.Color.r + e.Color.g + e.Color.b)
			.ThenBy(e => e.Color.r)
			.ThenBy(e => e.Color.g)
			.ThenBy(e => e.Color.b)
			.Select(e => e.Marker)
			.ToList();

		if (allMarkers.Count == 0)
			return;

		const int markerSize = 9;
		var availableHeight = box.rect.h + 4;
		var spacing = -2;

		while (allMarkers.Count * markerSize + (allMarkers.Count - 1) * spacing > availableHeight && markerSize + spacing > 2)
			spacing--;
		
		RenderMarkers(allMarkers, new Vec(box.rect.x2 - 7, box.rect.y - 2), new Vec(0, markerSize + spacing));
	}

	private static void CardBrowse_Render_Postfix(CardBrowse __instance, G g)
	{
		if (__instance.subRoute is not null)
			return;
		if (__instance.browseAction is not null)
			return;
		if (__instance.browseSource == CardBrowse.Source.Codex)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled)
			return;

		var selectedColorIndex = __instance.GetSelectedColorIndex();
		var selectedMarkerType = __instance.GetSelectedMarkerType();
		var selectedColor = selectedColorIndex < MarkerColors.Length ? MarkerColors[selectedColorIndex] : DB.decks[g.state.characters[selectedColorIndex - MarkerColors.Length].deckType ?? Deck.colorless].color;
		var maxY = 0.0;

		var totalColors = MarkerColors.Length + (g.state.IsOutsideRun() ? 0 : g.state.characters.Count);
		for (var i = 0; i < totalColors; i++)
		{
			var markerColor = i < MarkerColors.Length ? MarkerColors[i] : DB.decks[g.state.characters[i - MarkerColors.Length].deckType ?? Deck.colorless].color;
			var isSelected = selectedColorIndex == i;
			
			var colorBox = g.Push(new UIKey(CardMarkerColorUk, i), new Rect(87, 54 + i * 9, 8, 8), onMouseDown: __instance);
			
			Draw.Rect(colorBox.rect.x, colorBox.rect.y, colorBox.rect.w, colorBox.rect.h, markerColor.fadeAlpha(isSelected ? 1 : 0.5));

			if (colorBox.IsHover())
				g.tooltips.Add(new Vec(colorBox.rect.x2 + 4, colorBox.rect.y), new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}")
				{
					Icon = MarkerIcons[selectedMarkerType ?? MarkerType.Cross].Sprite,
					IconColor = markerColor,
					TitleColor = Colors.white,
					Title = ModEntry.Instance.Localizations.Localize(["CardMarkers", "Tooltip", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardMarkers", "Tooltip", "Description"]),
				});

			maxY = Math.Max(maxY, colorBox.rect.y2);
			g.Pop();
		}

		foreach (var (markerType, index) in Enum.GetValues<MarkerType>().Select((type, index) => (Type: type, Index: index)))
		{
			var isSelected = selectedMarkerType == markerType;
			
			var markerBox = g.Push(new UIKey(CardMarkerTypeUk, (int)markerType), new Rect(96, 54 + index * 11, 9, 9), onMouseDown: __instance);
			
			Draw.Sprite(MarkerIcons[markerType].Sprite, markerBox.rect.x, markerBox.rect.y, color: selectedColor.fadeAlpha(isSelected ? 1 : 0.5));
			
			if (markerBox.IsHover())
				g.tooltips.Add(new Vec(markerBox.rect.x2 + 4, markerBox.rect.y), new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}")
				{
					Icon = MarkerIcons[markerType].Sprite,
					IconColor = selectedColor,
					TitleColor = Colors.white,
					Title = ModEntry.Instance.Localizations.Localize(["CardMarkers", "Tooltip", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardMarkers", "Tooltip", "Description"]),
				});
			
			maxY = Math.Max(maxY, markerBox.rect.y2);
			g.Pop();
		}

		var clearBox = g.Push(ClearCardMarkersUk, new Rect(91, maxY + 4, 10, 10), onMouseDown: __instance);
		
		Draw.Sprite(StableSpr.icons_x_white, clearBox.rect.x, clearBox.rect.y, color: Colors.white.fadeAlpha(__instance.IsClearMarkersSelected() ? 1 : 0.5));
		
		if (clearBox.IsHover())
			g.tooltips.Add(new Vec(clearBox.rect.x2 + 4, clearBox.rect.y), new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}")
			{
				TitleColor = Colors.white,
				Title = ModEntry.Instance.Localizations.Localize(["CardMarkers", "ClearTooltip", "Title"]),
				Description = ModEntry.Instance.Localizations.Localize(["CardMarkers", "ClearTooltip", "Description"]),
			});
		
		g.Pop();
	}

	private static bool CardBrowse_OnMouseDown_Prefix(CardBrowse __instance, G g, Box b)
	{
		if (__instance.subRoute is not null)
			return true;
		if (__instance.browseAction is not null)
			return true;
		if (__instance.browseSource == CardBrowse.Source.Codex)
			return true;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled)
			return true;
		if (b.key is not { } key)
			return true;

		if (key.k == ClearCardMarkersUk)
		{
			__instance.ToggleClearMarkersSelected();
			__instance.SetSelectedMarkerType(null);
			Audio.Play(Event.Click);
			return false;
		}

		if (key.k == CardMarkerColorUk)
		{
			__instance.SetSelectedColorIndex(key.v);
			Audio.Play(Event.Click);
			return false;
		}
		
		if (key.k == CardMarkerTypeUk)
		{
			if (__instance.IsClearMarkersSelected())
				__instance.ToggleClearMarkersSelected();
			
			var values = Enum.GetValues<MarkerType>();
			var markerType = values[Math.Clamp(key.v, 0, values.Length - 1 + (g.state.IsOutsideRun() ? 0 : g.state.characters.Count))];
			
			if (__instance.GetSelectedMarkerType() == markerType)
				__instance.SetSelectedMarkerType(null);
			else
				__instance.SetSelectedMarkerType(markerType);
			
			Audio.Play(Event.Click);
			return false;
		}

		if (key.k == StableUK.card && !Input.shift && !Input.ctrl && !Input.alt)
		{
			if (__instance.IsClearMarkersSelected())
			{
				__instance.GetCardList(g).FirstOrDefault(card => card.uuid == key.v)?.ClearMarkers();
				Audio.Play(Event.Click);
				return false;
			}
			if (__instance.GetSelectedMarkerType() is { } selectedMarkerType)
			{
				var selectedColorIndex = __instance.GetSelectedColorIndex();
				var markerColor = selectedColorIndex < MarkerColors.Length ? MarkerColors[selectedColorIndex] : DB.decks[g.state.characters[selectedColorIndex - MarkerColors.Length].deckType ?? Deck.colorless].color;
				__instance.GetCardList(g).FirstOrDefault(card => card.uuid == key.v)?.ToggleMarker(selectedMarkerType, markerColor);
				Audio.Play(Event.Click);
				return false;
			}
		}

		return true;
	}

	private static void Card_Render_Postfix(Card __instance, G g, Vec? posOverride)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CardMarkers.IsEnabled)
			return;
		if (__instance.GetMarkers() is not { } markers)
			return;

		var box = g.Push();
		
		var position = posOverride ?? __instance.pos;
		position += new Vec(box.rect.x, box.rect.y + __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim));
		position = position.round();

		RenderMarkers(markers, new Vec(position.x, position.y + 75), new Vec(7));

		g.Pop();
	}

	private static void Combat_RenderDeck_Postfix(G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderMarkerOverlayIfNeeded(g, StableUK.combat_deck, g.state.deck);
	}

	private static void Combat_RenderDiscard_Postfix(Combat __instance, G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderMarkerOverlayIfNeeded(g, StableUK.combat_discard, __instance.discard);
	}

	private static void Combat_RenderExhaust_Postfix(Combat __instance, G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderMarkerOverlayIfNeeded(g, StableUK.combat_exhaust, __instance.exhausted);
	}
}