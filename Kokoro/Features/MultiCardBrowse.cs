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
	partial class ActionApiImplementation
	{
		public IKokoroApi.IActionApi.IMultiCardBrowse MultiCardBrowse
			=> new MultiCardBrowseImplementation();

		public sealed class MultiCardBrowseImplementation : IKokoroApi.IActionApi.IMultiCardBrowse
		{
			public IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute MakeRoute(Action<CardBrowse>? @delegate = null)
			{
				var route = new MultiCardBrowseManager.MultiCardBrowse();
				@delegate?.Invoke(route);
				return route;
			}

			public IReadOnlyList<Card>? GetSelectedCards(CardAction action)
				=> ModEntry.Instance.Helper.ModData.GetOptionalModData<IReadOnlyList<Card>>(action, "SelectedCards");

			public IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction MakeCustomAction(CardAction action, string title)
				=> new CustomAction(action, title);
		}

		public sealed class CustomAction(
			CardAction action,
			string title
		) : IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction
		{
			public CardAction Action { get; set; } = action;
			public string Title { get; set; } = title;
			public int MinSelected { get; set; }
			public int MaxSelected { get; set; } = int.MaxValue;
			
			public IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction SetAction(CardAction value)
			{
				this.Action = value;
				return this;
			}

			public IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction SetTitle(string value)
			{
				this.Title = value;
				return this;
			}

			public IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction SetMinSelected(int value)
			{
				this.MinSelected = value;
				return this;
			}

			public IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction SetMaxSelected(int value)
			{
				this.MaxSelected = value;
				return this;
			}
		}
	}
}

internal sealed class MultiCardBrowseManager
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

	internal sealed class MultiCardBrowse : CardBrowse, OnMouseDown, IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute
	{
		private static readonly UK ChooseUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		
		public IReadOnlyList<IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction>? CustomActions { get; set; }
		public int MinSelected { get; set; }
		public int MaxSelected { get; set; } = int.MaxValue;
		public bool EnabledSorting { get; set; } = true;
		public bool BrowseActionIsOnlyForTitle { get; set; }
		public IReadOnlyList<Card>? CardsOverride { get; set; }
		
		internal readonly HashSet<int> SelectedCards = [];

		public MultiCardBrowse()
		{
			mode = Mode.Browse;
		}
		
		CardBrowse IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.AsRoute
			=> this;

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.ModifyRoute(Action<CardBrowse> @delegate)
		{
			@delegate(this);
			return this;
		}

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.SetCustomActions(IReadOnlyList<IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction>? value)
		{
			this.CustomActions = value;
			return this;
		}

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.SetMinSelected(int value)
		{
			this.MinSelected = value;
			return this;
		}

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.SetMaxSelected(int value)
		{
			this.MaxSelected = value;
			return this;
		}

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.SetEnabledSorting(bool value)
		{
			this.EnabledSorting = value;
			return this;
		}

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.SetBrowseActionIsOnlyForTitle(bool value)
		{
			this.BrowseActionIsOnlyForTitle = value;
			return this;
		}

		IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute IKokoroApi.IActionApi.IMultiCardBrowse.IMultiCardBrowseRoute.SetCardsOverride(IReadOnlyList<Card>? value)
		{
			this.CardsOverride = value;
			return this;
		}
		
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
				var inactive = SelectedCards.Count < action.MinSelected || SelectedCards.Count > action.MaxSelected;
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
				if (!SelectedCards.Remove(uuid))
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
		
		private List<IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction> GetAllActions()
		{
			var results = new List<IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction>(Math.Max((browseAction is null ? 0 : 1) + (CustomActions?.Count ?? 0), 1));
			if (CustomActions is not null)
				results.AddRange(CustomActions);
			if ((browseAction is not null && !BrowseActionIsOnlyForTitle) || results.Count == 0)
				results.Add(new ApiImplementation.ActionApiImplementation.CustomAction(browseAction ?? new ADummyAction(), I18n.MultiCardBrowseDoneButtonTitle).SetMinSelected(MinSelected).SetMaxSelected(MaxSelected));
			return results;
		}
		
		private void Finish(G g, IKokoroApi.IActionApi.IMultiCardBrowse.ICustomAction action)
		{
			if (SelectedCards.Count < action.MinSelected || SelectedCards.Count > action.MaxSelected)
			{
				Audio.Play(Event.ZeroEnergy);
				return;
			}
			
			ModEntry.Instance.Helper.ModData.SetModData<IReadOnlyList<Card>>(action.Action, "SelectedCards", _listCache.Where(card => SelectedCards.Contains(card.uuid)).ToList());
			g.state.GetCurrentQueue().QueueImmediate(action.Action);
			g.CloseRoute(this, CBResult.Done);
		}
	}
}