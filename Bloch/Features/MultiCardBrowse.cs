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

	private readonly HashSet<int> SelectedCards = [];

	private static void SetupHarmonyIfNeeded()
	{
		if (IsHarmonySetup)
			return;

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

		CurrentlyRenderedMenu = this;
		base.Render(g);
		CurrentlyRenderedMenu = null;

		SharedArt.ButtonText(
			g,
			new Vec(390, GetBackButtonMode() == BackMode.None ? 228 : 202),
			(UIKey)(UK)21375001,
			ModEntry.Instance.Localizations.Localize(["route", "MultiCardBrowse", "doneButton"]),
			inactive: SelectedCards.Count < MinSelected || SelectedCards.Count > MaxSelected,
			onMouseDown: this,
			platformButtonHint: Btn.Y
		);
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey((UK)21370001)) is { } box)
			box.onInputPhase = new InputPhaseHandler(() =>
			{
				if (Input.GetGpDown(Btn.Y))
					Finish(g);
			});
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

	private static void Card_Render_Prefix(Card __instance, ref bool hilight)
	{
		if (CurrentlyRenderedMenu is null)
			return;
		if (CurrentlyRenderedMenu.SelectedCards.Contains(__instance.uuid))
			hilight = true;
	}
}
