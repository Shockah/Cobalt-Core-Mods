using FSPRO;
using HarmonyLib;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IMultiCardBrowseApi MultiCardBrowse { get; } = new MultiCardBrowseApi();
		
		public sealed class MultiCardBrowseApi : IKokoroApi.IV2.IMultiCardBrowseApi
		{
			public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute? AsRoute(CardBrowse route)
				=> route as IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute;

			public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute MakeRoute(CardBrowse route)
			{
				var multiCardBrowse = new MultiCardBrowseManager.MultiCardBrowse();
				multiCardBrowse.SetupFromRoute(route);
				return multiCardBrowse;
			}

			public IReadOnlyList<Card>? GetSelectedCards(CardAction action)
				=> ModEntry.Instance.Helper.ModData.GetOptionalModData<IReadOnlyList<Card>>(action, "SelectedCards");

			public void SetSelectedCards(CardAction action, IEnumerable<Card>? cards)
				=> ModEntry.Instance.Helper.ModData.SetOptionalModData<IReadOnlyList<Card>>(action, "SelectedCards", cards?.ToList());

			public IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction MakeCustomAction(CardAction action, string title)
				=> new CustomAction(action, title);
			
			public sealed class CustomAction(
				CardAction action,
				string title
			) : IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction
			{
				public CardAction Action { get; set; } = action;
				public string Title { get; set; } = title;
				public int? MinSelected { get; set; }
				public int? MaxSelected { get; set; }
			
				public IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction SetAction(CardAction value)
				{
					this.Action = value;
					return this;
				}

				public IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction SetTitle(string value)
				{
					this.Title = value;
					return this;
				}

				public IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction SetMinSelected(int? value)
				{
					this.MinSelected = value;
					return this;
				}

				public IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction SetMaxSelected(int? value)
				{
					this.MaxSelected = value;
					return this;
				}
			}
		}
	}
}

internal static class MultiCardBrowseManager
{
	private static MultiCardBrowse? CurrentlyRenderedMenu;
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Loc), nameof(Loc.GetLocString)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Loc_GetLocString_Postfix))
		);
	}
	
	private static bool CardBrowse_GetCardList_Prefix(CardBrowse __instance, ref List<Card> __result)
	{
		if (__instance is not MultiCardBrowse route)
			return true;

		if (route.CardsOverride is null)
			return true;

		__result = [.. route.CardsOverride];
		__instance._listCache.Clear();
		__instance._listCache.AddRange(route.CardsOverride);
		return false;
	}

	private static void Card_Render_Prefix(Card __instance, ref bool hilight)
	{
		if (CurrentlyRenderedMenu is null)
			return;
		if (CurrentlyRenderedMenu.SelectedCards.Contains(__instance.uuid))
			hilight = true;
	}

	private static void Loc_GetLocString_Postfix(string key, ref string __result)
	{
		if (CurrentlyRenderedMenu is not { } menu)
			return;
		if (menu.EnabledSorting)
			return;
		if (key == "codex.sortBy")
			__result = "";
	}

	internal sealed class MultiCardBrowse : CardBrowse, OnMouseDown, IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute
	{
		private static readonly UK ChooseUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		
		public IReadOnlyList<IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction>? CustomActions { get; set; }
		public int MinSelected { get; set; }
		public int MaxSelected { get; set; } = int.MaxValue;
		public bool EnabledSorting { get; set; } = true;
		public bool BrowseActionIsOnlyForTitle { get; set; }
		public IReadOnlyList<Card>? CardsOverride { get; set; }
		
		internal readonly List<int> SelectedCards = [];

		public MultiCardBrowse()
		{
			mode = Mode.Browse;
		}
		
		public CardBrowse AsRoute
			=> this;
		
		public override void Render(G g)
		{
			var oldEnabledSortModes = enabledSortModes.ToList();

			CurrentlyRenderedMenu = this;
			if (!EnabledSorting)
				enabledSortModes.Clear();
			base.Render(g);
			if (!EnabledSorting)
				enabledSortModes.AddRange(oldEnabledSortModes);
			CurrentlyRenderedMenu = null;

			var allActions = GetAllActions();
			for (var i = 0; i < allActions.Count; i++)
			{
				var action = allActions[i];
				var inactive = SelectedCards.Count < (action.MinSelected ?? MinSelected) || SelectedCards.Count > (action.MaxSelected ?? MaxSelected);
				SharedArt.ButtonText(
					g,
					new Vec(390, (GetBackButtonMode() == BackMode.None ? 228 : 202) - (allActions.Count - 1 - i) * 26),
					new UIKey(ChooseUk, i),
					action.Title,
					boxColor: inactive ? Colors.buttonInactive : null,
					inactive: inactive,
					onMouseDown: this
				);
			}
		}
		
		void OnMouseDown.OnMouseDown(G g, Box b)
		{
			if (b.key?.ValueFor(StableUK.card) is { } uuid)
			{
				if (mode != Mode.Browse || browseAction is null || _listCache.FirstOrDefault(card => card.uuid == uuid) is null)
				{
					this.OnMouseDown(g, b);
					return;
				}

				Audio.Play(Event.Click);
				if (SelectedCards.Contains(uuid))
					SelectedCards.Remove(uuid);
				else
					SelectedCards.Add(uuid);
			}
			else if (b.key?.k == (UIKey)ChooseUk)
			{
				var actions = GetAllActions();
				Finish(g, actions[b.key?.v ?? 0]);
			}
			else
			{
				this.OnMouseDown(g, b);
			}
		}
		
		private List<IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction> GetAllActions()
		{
			var results = new List<IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction>(Math.Max((browseAction is null ? 0 : 1) + (CustomActions?.Count ?? 0), 1));
			if (CustomActions is not null)
				results.AddRange(CustomActions);
			if ((browseAction is not null && !BrowseActionIsOnlyForTitle) || results.Count == 0)
				results.Add(new ApiImplementation.V2Api.MultiCardBrowseApi.CustomAction(browseAction ?? new ADummyAction(), ModEntry.Instance.Localizations.Localize(["multiCardBrowse", "done"])).SetMinSelected(MinSelected).SetMaxSelected(MaxSelected));
			return results;
		}
		
		private void Finish(G g, IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction action)
		{
			if (SelectedCards.Count < (action.MinSelected ?? MinSelected) || SelectedCards.Count > (action.MaxSelected ?? MaxSelected))
			{
				Audio.Play(Event.ZeroEnergy);
				return;
			}
			
			ModEntry.Instance.Helper.ModData.SetModData<IReadOnlyList<Card>>(action.Action, "SelectedCards", SelectedCards.Select(uuid => _listCache.FirstOrDefault(card => card.uuid == uuid)).OfType<Card>().ToList());
			g.state.GetCurrentQueue().QueueImmediate(action.Action);
			g.CloseRoute(this, CBResult.Done);
		}

		internal void SetupFromRoute(CardBrowse route)
		{
			{
				allowCancel = route.allowCancel;
				allowCloseOverride = route.allowCloseOverride;
				mode = route.mode;
				browseSource = route.browseSource;
				browseAction = route.browseAction;
				sortMode = route.sortMode;
				filterUnremovableAtShops = route.filterUnremovableAtShops;
				filterExhaust = route.filterExhaust;
				filterRetain = route.filterRetain;
				filterBuoyant = route.filterBuoyant;
				filterTemporary = route.filterTemporary;
				includeTemporaryCards = route.includeTemporaryCards;
				filterOutTheseRarities = route.filterOutTheseRarities?.ToList();
				filterMinCost = route.filterMinCost;
				filterMaxCost = route.filterMaxCost;
				filterUpgrade = route.filterUpgrade;
				filterAvailableUpgrade = route.filterAvailableUpgrade;
				filterUUID = route.filterUUID;
				ignoreCardType = route.ignoreCardType;
				hideUnknownCards = route.hideUnknownCards;
			}

			if (route is MultiCardBrowse multiCardBrowse)
			{
				CustomActions = multiCardBrowse.CustomActions is null ? null : Mutil.DeepCopy(multiCardBrowse.CustomActions);
				MinSelected = multiCardBrowse.MinSelected;
				MaxSelected = multiCardBrowse.MaxSelected;
				EnabledSorting = multiCardBrowse.EnabledSorting;
				BrowseActionIsOnlyForTitle = multiCardBrowse.BrowseActionIsOnlyForTitle;
				CardsOverride = multiCardBrowse.CardsOverride is null ? null : Mutil.DeepCopy(multiCardBrowse.CardsOverride);
			}
			
			ModEntry.Instance.ExtensionDataManager.CopyAllModData(route, this);
			ModEntry.Instance.Helper.ModData.CopyAllModData(route, this);
		}
		
		public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute SetCustomActions(IReadOnlyList<IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction>? value)
		{
			this.CustomActions = value;
			return this;
		}
				
		public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute SetMinSelected(int value)
		{
			this.MinSelected = value;
			return this;
		}
				
		public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute SetMaxSelected(int value)
		{
			this.MaxSelected = value;
			return this;
		}
				
		public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute SetEnabledSorting(bool value)
		{
			this.EnabledSorting = value;
			return this;
		}
				
		public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute SetBrowseActionIsOnlyForTitle(bool value)
		{
			this.BrowseActionIsOnlyForTitle = value;
			return this;
		}
				
		public IKokoroApi.IV2.IMultiCardBrowseApi.IMultiCardBrowseRoute SetCardsOverride(IReadOnlyList<Card>? value)
		{
			this.CardsOverride = value;
			return this;
		}
	}
}