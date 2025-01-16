using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool BrowseCardPilesDuringPeek = true;
}

internal sealed class BrowseCardPilesDuringPeek : IRegisterable
{
	// private static readonly UK FakeCombatRootBoxUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	private static bool ShouldRenderCardPileButtonAnyway;
	private static readonly ConditionalWeakTable<Combat, InputHandler> InputHandlers = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDeck)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDeck_Prefix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDiscard)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDiscard_Prefix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderExhaust)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderExhaust_Prefix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Render)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Render_Postfix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryCloseSubRoute)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryCloseSubRoute_Prefix_First)), priority: Priority.First),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryCloseSubRoute_Postfix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.CanBePeeked)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_CanBePeeked_Postfix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["BrowseCardPilesDuringPeek", "settings", "header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["BrowseCardPilesDuringPeek", "settings", "enabled", "title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.BrowseCardPilesDuringPeek,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.BrowseCardPilesDuringPeek = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.BrowseCardPilesDuringPeek)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["BrowseCardPilesDuringPeek", "settings", "enabled", "title"]),
					Description = ModEntry.Instance.Localizations.Localize(["BrowseCardPilesDuringPeek", "settings", "enabled", "description"]),
				},
			]),
		]);
	
	private static bool ShouldHideOriginalAndDisplayCustomCardPileButton(Combat combat)
	{
		// hide original card pile button if we have a subroute - we'll draw one on top of the subroute later
		if (!ModEntry.Instance.Settings.ProfileBased.Current.BrowseCardPilesDuringPeek)
			return false;
		if (combat.routeOverride is not { } routeOverride)
			return false;
		if (!routeOverride.CanBePeeked())
			return false;
		if (!combat.eyeballPeek)
			return false;
		return true;
	}

	private static bool Combat_RenderDeck_Prefix_First(Combat __instance)
		=> ShouldRenderCardPileButtonAnyway || !ShouldHideOriginalAndDisplayCustomCardPileButton(__instance);

	private static bool Combat_RenderDiscard_Prefix_First(Combat __instance)
		=> ShouldRenderCardPileButtonAnyway || !ShouldHideOriginalAndDisplayCustomCardPileButton(__instance);

	private static bool Combat_RenderExhaust_Prefix_First(Combat __instance)
		=> ShouldRenderCardPileButtonAnyway || !ShouldHideOriginalAndDisplayCustomCardPileButton(__instance);

	private static void Combat_Render_Postfix_First(Combat __instance, G g)
	{
		if (!ShouldHideOriginalAndDisplayCustomCardPileButton(__instance))
			return;
		if (g.boxes.FirstOrDefault(b => b.key == StableUK.combat_root) is null)
			return;

		if (!InputHandlers.TryGetValue(__instance, out var inputHandler))
		{
			inputHandler = new(__instance);
			InputHandlers.AddOrUpdate(__instance, inputHandler);
		}

		try
		{
			ShouldRenderCardPileButtonAnyway = true;
			
			g.Push(rect: new Rect(Combat.marginRect.x, Combat.marginRect.y));
			
			__instance.RenderDeck(g);
			if (g.boxes.FirstOrDefault(b => b.key == StableUK.combat_deck) is { } deckBox)
				deckBox.onMouseDown = inputHandler;
			
			__instance.RenderDiscard(g);
			if (g.boxes.FirstOrDefault(b => b.key == StableUK.combat_discard) is { } discardBox)
				discardBox.onMouseDown = inputHandler;
			
			__instance.RenderExhaust(g);
			if (g.boxes.FirstOrDefault(b => b.key == StableUK.combat_exhaust) is { } exhaustBox)
				exhaustBox.onMouseDown = inputHandler;
			
			g.Pop();
		}
		finally
		{
			ShouldRenderCardPileButtonAnyway = false;
		}
	}

	private static void Combat_TryCloseSubRoute_Prefix_First(Combat __instance, out bool __state)
		=> __state = __instance.routeOverride is null;

	private static void Combat_TryCloseSubRoute_Postfix_First(Combat __instance, Route r, ref bool __state, in bool __result)
	{
		if (!__result)
			return;
		if (__state || __instance.routeOverride is not null)
			return;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Route>(r, "OriginalRouteOverride", out var originalRouteOverride))
			return;

		__instance.routeOverride = originalRouteOverride;
		__instance.eyeballPeek = true;
	}

	private static void CardBrowse_CanBePeeked_Postfix(CardBrowse __instance, ref bool __result)
	{
		if (!__result)
			return;
		if (!ModEntry.Instance.Helper.ModData.ContainsModData(__instance, "OriginalRouteOverride"))
			return;
		
		__result = false;
	}

	private sealed class InputHandler(Combat combat) : OnMouseDown
	{
		public void OnMouseDown(G g, Box b)
		{
			if (combat.routeOverride is not { } routeOverride)
				return;
			if (b.key != StableUK.combat_deck && b.key != StableUK.combat_discard && b.key != StableUK.combat_exhaust)
				return;
			
			combat.OnMouseDown(g, b);

			if (combat.routeOverride is not { } newRouteOverride || newRouteOverride == routeOverride)
				return;
			
			combat.eyeballPeek = false;
			ModEntry.Instance.Helper.ModData.SetModData(newRouteOverride, "OriginalRouteOverride", routeOverride);
		}
	}
}