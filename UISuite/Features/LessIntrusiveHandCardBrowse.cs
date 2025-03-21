using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using daisyowl.text;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool LessIntrusiveHandCardBrowse = true;
}

internal sealed class LessIntrusiveHandCardBrowse : IRegisterable
{
	private static bool ShouldRenderCardAnyway;
	private static readonly ConditionalWeakTable<CardBrowse, List<Card>> CardBrowseCardListCache = [];

	private static readonly Lazy<Func<UK>?> MultiCardBrowseChooseUkGetter = new(() =>
	{
		if (ModEntry.Instance.KokoroApi is null)
			return null;

		var multiCardBrowseType = AccessTools.AllAssemblies()
			.First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro")
			.GetType("Shockah.Kokoro.MultiCardBrowseManager")!
			.GetNestedType("MultiCardBrowse", AccessTools.all)!;

		return AccessTools.DeclaredField(multiCardBrowseType, "ChooseUk").EmitStaticGetter<UK>();
	});
	
	private static readonly Lazy<Func<CardBrowse, List<int>>?> MultiCardBrowseSelectedCardsGetter = new(() =>
	{
		if (ModEntry.Instance.KokoroApi is null)
			return null;

		var multiCardBrowseType = AccessTools.AllAssemblies()
			.First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro")
			.GetType("Shockah.Kokoro.MultiCardBrowseManager")!
			.GetNestedType("MultiCardBrowse", AccessTools.all)!;

		var method = new DynamicMethod("get_MultiCardBrowse_SelectedCards", typeof(List<int>), [typeof(CardBrowse)]);
		var il = method.GetILGenerator();

		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Castclass, multiCardBrowseType);
		il.Emit(OpCodes.Ldfld, AccessTools.DeclaredField(multiCardBrowseType, "SelectedCards"));
		il.Emit(OpCodes.Ret);

		return method.CreateDelegate<Func<CardBrowse, List<int>>>();
	});
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.IsVisible)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_IsVisible_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Prefix))
		);
		if (ModEntry.Instance.KokoroApi is not null)
			ModEntry.Instance.Harmony.Patch(
				original: AccessTools.AllAssemblies()
					.First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro")
					.GetType("Shockah.Kokoro.MultiCardBrowseManager")!
					.GetNestedType("MultiCardBrowse", AccessTools.all)!
					.GetMethod("Render", AccessTools.all)!,
				prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MultiCardBrowse_Render_Prefix))
			);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix))
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
					() => ModEntry.Instance.Localizations.Localize(["LessIntrusiveHandCardBrowse", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["LessIntrusiveHandCardBrowse", "Settings", "IsEnabled", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.LessIntrusiveHandCardBrowse,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.LessIntrusiveHandCardBrowse = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.LessIntrusiveHandCardBrowse)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["LessIntrusiveHandCardBrowse", "Settings", "IsEnabled", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["LessIntrusiveHandCardBrowse", "Settings", "IsEnabled", "Description"]),
				},
			]),
		]);

	internal static bool CanUseLessIntrusiveUI(CardBrowse route, G g)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.LessIntrusiveHandCardBrowse)
			return false;
		// if (route is not CardBrowse)
		// 	return false;
		if (route.subRoute is not null)
			return false;
		if (route.browseSource != CardBrowse.Source.Hand)
			return false;
		if (g.state.route is not Combat combat)
			return false;

		if (!CardBrowseCardListCache.TryGetValue(route, out var cardList))
		{
			cardList = route.GetCardList(g);
			CardBrowseCardListCache.AddOrUpdate(route, cardList);
		}
		
		if (cardList.Any(card => !combat.hand.Contains(card)))
			return false;
		return true;
	}

	private static void RenderCardBrowse(CardBrowse route, G g, Combat combat, List<Card> cards, List<int>? selectedCards)
	{
		var box = g.Push();
		
		Draw.Rect(0, 0, MG.inst.PIX_W, MG.inst.PIX_H, Colors.black.fadeAlpha(0.5));
		
		if (GetBrowseTitle() is { } browseTitle)
			Draw.Text(browseTitle, box.rect.x + 240, box.rect.y + 24, color: Colors.textMain, align: TAlign.Center, outline: Colors.black);
		
		if (route.browseAction?.GetCardSelectText(g.state) is { } browseSubtitle)
			Draw.Text(browseSubtitle, box.rect.x + 240, box.rect.y + 34, color: Colors.textBold, maxWidth: 300, align: TAlign.Center, outline: Colors.black);
		else
			Draw.Text(GetUpgradeHintSubtitle(), box.rect.x + 240, box.rect.y + 34, color: Colors.textMain.gain(0.5), maxWidth: 300, align: TAlign.Center, outline: Colors.black);

		RenderCards(false);
		RenderCards(true);

		switch (route.GetBackButtonMode())
		{
			case CardBrowse.BackMode.Done:
				SharedArt.ButtonText(
					g,
					new Vec(MG.inst.PIX_W - 69, MG.inst.PIX_H - 31),
					StableUK.cardbrowse_back,
					Loc.T("uiShared.btnBack"),
					onMouseDown: route,
					platformButtonHint: Btn.B
				);
				break;
			case CardBrowse.BackMode.Cancel:
				SharedArt.ButtonText(
					g,
					new Vec(MG.inst.PIX_W - 69, MG.inst.PIX_H - 31),
					StableUK.cardbrowse_cancel,
					Loc.T("uiShared.btnCancel"),
					onMouseDown: route,
					platformButtonHint: Btn.B
				);
				break;
		}

		g.Pop();
		
		string? GetBrowseTitle()
			=> route.mode switch
			{
				CardBrowse.Mode.Browse => Loc.T("cardBrowse.title.hand", "Hand: {0} Cards", cards.Count),
				CardBrowse.Mode.DeleteCard => Loc.T("cardBrowse.title.delete"),
				CardBrowse.Mode.UpgradeCard => Loc.T("cardBrowse.title.upgrade"),
				_ => null
			};

		string GetUpgradeHintSubtitle()
		{
			if (PlatformIcons.GetPlatform() == Platform.MouseKeyboard)
				return Loc.T("cardReward.howToPreviewUpgrade");

			var controllerButton = PlatformIcons.GetPlatform() switch
			{
				Platform.Xbox => Loc.T("controller.xbox.xMuted"),
				Platform.NX => Loc.T("controller.nx.x"),
				Platform.PS => Loc.T("controller.ps.square"),
				_ => Loc.T("controller.xbox.xMuted"),
			};
			return Loc.T("cardReward.howToPreviewUpgrade.controller", true, controllerButton);
		}

		void RenderCards(bool inForeground)
		{
			try
			{
				ShouldRenderCardAnyway = true;
				
				foreach (var card in cards)
				{
					if (card.isForeground != inForeground)
						continue;
				
					card.Render(
						g,
						new Vec(card.pos.x + Combat.marginRect.x, card.pos.y + Combat.marginRect.y),
						ignoreAnim: true,
						hilight: selectedCards is not null && selectedCards.Contains(card.uuid),
						autoFocus: cards[0] == card,
						onMouseDown: route,
						onMouseDownRight: route,
						overrideWidth: combat.hand.Count >= 9 && combat.hand.IndexOf(card) < combat.hand.Count - 1 ? 50.0 : null,
						leftHint: cards[0] == card ? StableUK.combat_deck : null,
						rightHint: cards[^1] == card ? StableUK.combat_exhaust : null,
						isInCombatHand: true
					);
				}
			}
			finally
			{
				ShouldRenderCardAnyway = false;
			}
		}
	}

	private static void Combat_IsVisible_Postfix(Combat __instance, ref bool __result)
	{
		if (__instance.routeOverride is CardBrowse route && CanUseLessIntrusiveUI(route, MG.inst.g))
			__result = true;
	}

	private static bool CardBrowse_Render_Prefix(CardBrowse __instance, G g)
	{
		if (!CanUseLessIntrusiveUI(__instance, g))
			return true;
		if (g.state.route is not Combat combat)
			return true;
		
		var cards = __instance.GetCardList(g).OrderBy(card => combat.hand.IndexOf(card)).ToList();
		if (cards.Count == 0)
		{
			g.CloseRoute(__instance, CBResult.Done);
			return false;
		}

		if (combat.currentCardAction is { } currentCardAction)
			currentCardAction.timer = 0;
		
		RenderCardBrowse(__instance, g, combat, cards, null);
		return false;
	}

	private static bool MultiCardBrowse_Render_Prefix(CardBrowse __instance, G g)
	{
		if (!CanUseLessIntrusiveUI(__instance, g))
			return true;
		if (g.state.route is not Combat combat)
			return true;
		if (ModEntry.Instance.KokoroApi!.MultiCardBrowse.AsRoute(__instance) is not { } route)
			return true;
		
		var cards = __instance.GetCardList(g).OrderBy(card => combat.hand.IndexOf(card)).ToList();
		if (cards.Count == 0)
		{
			g.CloseRoute(__instance, CBResult.Done);
			return false;
		}
		
		var selectedCards = MultiCardBrowseSelectedCardsGetter.Value!(__instance);
		RenderCardBrowse(__instance, g, combat, cards, selectedCards);

		if (route.CustomActions is { } customActions)
		{
			for (var i = 0; i < customActions.Count; i++)
			{
				var action = customActions[i];
				var inactive = selectedCards.Count < (action.MinSelected ?? route.MinSelected) || selectedCards.Count > (action.MaxSelected ?? route.MaxSelected);
				SharedArt.ButtonText(
					g,
					new Vec(MG.inst.PIX_W - 69, 82 + i * 26),
					new UIKey(MultiCardBrowseChooseUkGetter.Value!(), i),
					action.Title,
					boxColor: inactive ? Colors.buttonInactive : null,
					inactive: inactive,
					onMouseDown: __instance
				);
			}
		}
		
		return false;
	}

	private static bool Card_Render_Prefix(Card __instance, G g)
	{
		if (ShouldRenderCardAnyway)
			return true;
		
		if (g.state.route is not Combat combat)
		{
			RevertAnimation();
			return true;
		}
		if (combat.routeOverride is not CardBrowse route)
		{
			RevertAnimation();
			return true;
		}
		if (!CanUseLessIntrusiveUI(route, g))
		{
			RevertAnimation();
			return true;
		}
		if (!combat.hand.Contains(__instance))
		{
			RevertAnimation();
			return true;
		}
		
		if (route.GetCardList(g).Contains(__instance))
			return false;
		
		__instance.targetPos.y += 20;
		Animate();

		return true;

		void Animate()
		{
			var pos = ModEntry.Instance.Helper.ModData.GetOptionalModData<Vec>(__instance, "PreLessIntrusiveHandCardBrowsePos") ?? __instance.pos;
			pos = Mutil.LerpDeltaSnap(pos, __instance.targetPos, 12.0, g.dt);
			__instance.pos = pos;
			ModEntry.Instance.Helper.ModData.SetModData(__instance, "PreLessIntrusiveHandCardBrowsePos", pos);
		}

		void RevertAnimation()
			=> ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "PreLessIntrusiveHandCardBrowsePos");
	}

	private static void CardBrowse_CanBePeeked_Postfix(CardBrowse __instance, ref bool __result)
	{
		if (!__result)
			return;
		if (MG.inst.g.state.route is not Combat combat)
			return;
		if (combat.routeOverride != __instance)
			return;
		if (!CanUseLessIntrusiveUI(__instance, MG.inst.g))
			return;
		
		__result = false;
	}
}