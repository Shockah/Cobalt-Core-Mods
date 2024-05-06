using FSPRO;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal static class CardActionExt
{
	public static List<Card> GetSelectedCards(this CardAction self)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<List<Card>>(self, "SelectedCards");
}

internal sealed class MultiCardBrowse : CardBrowse, OnMouseDown
{
	private static bool IsHarmonySetup = false;
	private static MultiCardBrowse? CurrentlyRenderedMenu;

	public int MinSelected = 0;
	public int MaxSelected = int.MaxValue;
	public bool EnabledSorting = true;
	public List<Card>? CardsOverride;

	private readonly HashSet<int> SelectedCards = [];

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

		IsHarmonySetup = true;
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
			Finish(g);
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

		var inactive = SelectedCards.Count < MinSelected || SelectedCards.Count > MaxSelected;
		SharedArt.ButtonText(
			g,
			new Vec(390, GetBackButtonMode() == BackMode.None ? 228 : 202),
			(UIKey)(UK)21375001,
			ModEntry.Instance.Localizations.Localize(["route", "MultiCardBrowse", "doneButton"]),
			boxColor: inactive ? Colors.buttonInactive : null, 
			inactive: inactive,
			onMouseDown: this
		);
	}

	private void Finish(G g)
	{
		if (SelectedCards.Count < MinSelected || SelectedCards.Count > MaxSelected)
		{
			Audio.Play(Event.ZeroEnergy);
			return;
		}

		if (browseAction is not null)
		{
			browseAction.GetSelectedCards().AddRange(_listCache.Where(card => SelectedCards.Contains(card.uuid)));
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

		__result = route.CardsOverride;
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
}
