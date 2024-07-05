using FSPRO;
using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MultiCardBrowse : CardBrowse, OnMouseDown, IBlochApi.IMultiCardBrowseRoute
{
	private static bool IsHarmonySetup = false;
	private static MultiCardBrowse? CurrentlyRenderedMenu;

	public IReadOnlyList<IBlochApi.IMultiCardBrowseRoute.CustomAction>? CustomActions { get; set; }
	public int MinSelected { get; set; } = 0;
	public int MaxSelected { get; set; } = int.MaxValue;
	public bool EnabledSorting { get; set; } = true;
	public bool BrowseActionIsOnlyForTitle { get; set; } = false;
	public IReadOnlyList<Card>? CardsOverride { get; set; }

	private readonly HashSet<int> SelectedCards = [];

	Route IBlochApi.IMultiCardBrowseRoute.AsRoute
		=> this;

	IBlochApi.IMultiCardBrowseRoute IBlochApi.IMultiCardBrowseRoute.SetCustomActions(IReadOnlyList<IBlochApi.IMultiCardBrowseRoute.CustomAction>? value)
	{
		this.CustomActions = value;
		return this;
	}

	IBlochApi.IMultiCardBrowseRoute IBlochApi.IMultiCardBrowseRoute.SetMinSelected(int value)
	{
		this.MinSelected = value;
		return this;
	}

	IBlochApi.IMultiCardBrowseRoute IBlochApi.IMultiCardBrowseRoute.SetMaxSelected(int value)
	{
		this.MaxSelected = value;
		return this;
	}

	IBlochApi.IMultiCardBrowseRoute IBlochApi.IMultiCardBrowseRoute.SetEnabledSorting(bool value)
	{
		this.EnabledSorting = value;
		return this;
	}

	IBlochApi.IMultiCardBrowseRoute IBlochApi.IMultiCardBrowseRoute.SetBrowseActionIsOnlyForTitle(bool value)
	{
		this.BrowseActionIsOnlyForTitle = value;
		return this;
	}

	IBlochApi.IMultiCardBrowseRoute IBlochApi.IMultiCardBrowseRoute.SetCardsOverride(IReadOnlyList<Card>? value)
	{
		this.CardsOverride = value;
		return this;
	}

	private static void SetupHarmonyIfNeeded()
	{
		if (IsHarmonySetup)
			return;

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(GetCardList)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Loc), nameof(Loc.GetLocString)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Loc_GetLocString_Postfix))
		);

		IsHarmonySetup = true;
	}

	private List<IBlochApi.IMultiCardBrowseRoute.CustomAction> GetAllActions()
	{
		var results = new List<IBlochApi.IMultiCardBrowseRoute.CustomAction>(Math.Max((browseAction is null ? 0 : 1) + (CustomActions?.Count ?? 0), 1));
		if (CustomActions is not null)
			results.AddRange(CustomActions);
		if ((browseAction is not null && !BrowseActionIsOnlyForTitle) || results.Count == 0)
			results.Add(new IBlochApi.IMultiCardBrowseRoute.CustomAction(browseAction, ModEntry.Instance.Localizations.Localize(["route", "MultiCardBrowse", "doneButton"]), MinSelected, MaxSelected));
		return results;
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
			if (!SelectedCards.Remove(uuid))
				SelectedCards.Add(uuid);
		}
		else if (b.key?.k == (UIKey)(UK)21375001)
		{
			var actions = GetAllActions();
			Finish(g, actions[b.key?.v ?? 0]);
		}
		else
		{
			this.OnMouseDown(g, b);
		}
	}

	public override void Render(G g)
	{
		SetupHarmonyIfNeeded();
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
			var inactive = SelectedCards.Count < action.MinSelected || SelectedCards.Count > action.MaxSelected;
			SharedArt.ButtonText(
				g,
				new Vec(390, (GetBackButtonMode() == BackMode.None ? 228 : 202) - (allActions.Count - 1 - i) * 26),
				new UIKey((UK)21375001, i),
				action.Title,
				boxColor: inactive ? Colors.buttonInactive : null,
				inactive: inactive,
				onMouseDown: this
			);
		}
	}

	private void Finish(G g, IBlochApi.IMultiCardBrowseRoute.CustomAction action)
	{
		if (SelectedCards.Count < action.MinSelected || SelectedCards.Count > action.MaxSelected)
		{
			Audio.Play(Event.ZeroEnergy);
			return;
		}

		if (action.Action is { } browseAction)
		{
			ModEntry.Instance.Helper.ModData.SetModData<IReadOnlyList<Card>>(browseAction, "SelectedCards", _listCache.Where(card => SelectedCards.Contains(card.uuid)).ToList());
			g.state.GetCurrentQueue().QueueImmediate(browseAction);
		}
		g.CloseRoute(this, CBResult.Done);
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
}
