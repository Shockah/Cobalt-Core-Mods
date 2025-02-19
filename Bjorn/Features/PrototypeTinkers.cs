using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class PrototypeTinkerManager : IRegisterable
{
	private static readonly Dictionary<string, TinkerEntry> UniqueNameToEntry = [];
	private static readonly OrderedList<TinkerEntry, double> OrderedEntries = new(false);
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	public static TinkerEntry RegisterEntry(string name, TinkerConfiguration configuration, double priority = 0)
	{
		var uniqueName = $"{ModEntry.Instance.Package.Manifest.UniqueName}::{name}";
		var entry = new TinkerEntry { ModOwner = ModEntry.Instance.Package.Manifest, UniqueName = uniqueName, Configuration = configuration };
		UniqueNameToEntry[uniqueName] = entry;
		OrderedEntries.Add(entry, priority);
		return entry;
	}

	public static TinkerEntry? LookupEntryByUniqueName(string uniqueName)
		=> UniqueNameToEntry.GetValueOrDefault(uniqueName);

	public static IEnumerable<Card> GetTinkerOptions(State state, PrototypeCard card)
		=> OrderedEntries
			.Where(e =>
			{
				if (card.Tinkers.FirstOrDefault(e2 => e2.TinkerUniqueName == e.UniqueName) is { } instanceEntry)
					return instanceEntry.Tinker.CanUpgradeTo(state, card, instanceEntry.Level + 1);

				var tinker = (ITinker)Activator.CreateInstance(e.Configuration.TinkerType)!;
				return tinker.CanUpgradeTo(state, card, 1);
			})
			.Select(e => e.Configuration.CardFactory(card))
			.Select(c =>
			{
				c.drawAnim = 1;
				return c;
			});

	public static int GetTinkerUpgradeCost(State state, PrototypeCard card)
		=> card.Tinkers.Sum(t => t.Tinker.GetTinkerCost(state, card, t.Level));
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterPrototypes", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterPrototypes"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, ref List<Card> __result)
	{
		var filterPrototypes = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterPrototypes");
		if (filterPrototypes is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterPrototypes is not null && __result[i] is PrototypeCard != filterPrototypes.Value)
				__result.RemoveAt(i);
	}
}

public sealed class TinkerEntry : IModOwned
{
	public required IModManifest ModOwner { get; init; }
	public required string UniqueName { get; init; }
	public required TinkerConfiguration Configuration { get; init; }
}

public struct TinkerConfiguration
{
	public required Type TinkerType { get; init; }
	public required Func<Card, Card> CardFactory { get; init; }
}

public interface ITinker
{
	bool CanUpgradeTo(State state, Card card, int level) => true;
	int GetTinkerCost(State state, Card card, int level) => level;
	string GetCardNameSuffix(State state, Card card, int level);
	void ModifyCardData(State state, Card card, int level, ref CardData data) { }
	IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state, Card card, int level) => ImmutableHashSet<ICardTraitEntry>.Empty;
	IEnumerable<CardAction> GetActions(State state, Combat combat, Card card, int level) => [];
}

internal sealed class LevelUpTinkerAction : CardAction
{
	public required int CardId;
	public required string TinkerUniqueName;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (s.FindCard(CardId) is not PrototypeCard card)
		{
			timer = 0;
			return;
		}

		if (card.Tinkers.FirstOrDefault(t => t.TinkerUniqueName == TinkerUniqueName) is { } instanceEntry)
		{
			instanceEntry.Level++;
			Audio.Play(Event.Status_PowerUp);
			return;
		}

		if (PrototypeTinkerManager.LookupEntryByUniqueName(TinkerUniqueName) is not { } entry)
		{
			timer = 0;
			return;
		}

		var tinker = (ITinker)Activator.CreateInstance(entry.Configuration.TinkerType)!;
		card.Tinkers.Add(new PrototypeCard.Entry { TinkerUniqueName = TinkerUniqueName, Tinker = tinker });
		Audio.Play(Event.Status_PowerUp);
	}
}

internal sealed class TinkerAnyAction : CardAction
{
	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		var baseResult = base.BeginWithRoute(g, s, c);

		var prototypes = c.hand.OfType<PrototypeCard>().ToList();

		switch (prototypes.Count)
		{
			case 0:
				timer = 0;
				return baseResult;
			case 1:
				timer = 0;
				return new TinkerAction { CardId = prototypes[0].uuid }.BeginWithRoute(g, s, c);
			default:
				var route = new CardBrowse { browseSource = CardBrowse.Source.Hand, browseAction = new TinkerAction() };
				ModEntry.Instance.Helper.ModData.SetModData(route, "FilterPrototypes", true);
				return route;
		}
	}
}

internal sealed class TinkerAction : CardAction
{
	public int CardId;

	public override string GetCardSelectText(State s)
		=> ModEntry.Instance.Localizations.Localize(["card", "Prototyping", "browsePrototypeTitle"]);

	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		var baseResult = base.BeginWithRoute(g, s, c);
		
		if ((selectedCard ?? s.FindCard(CardId)) is not PrototypeCard card)
		{
			timer = 0;
			return baseResult;
		}

		return new PlayCardReward
		{
			cards = PrototypeTinkerManager.GetTinkerOptions(s, card).ToList(),
			canSkip = true,
		};
	}

	private sealed class PlayCardReward : CardReward, OnMouseDown
	{
		public override void Render(G g)
		{
			base.Render(g);
			if (g.state.route is not Combat combat)
				return;
			if (combat.eyeballPeek)
				return;
			
			combat.pulseEnergyBad = Math.Max(0.0, combat.pulseEnergyBad - g.dt);

			g.Push(rect: Combat.marginRect);
			combat.RenderEnergy(g);
			g.Pop();
		}

		void OnMouseDown.OnMouseDown(G g, Box b)
		{
			if (b.key?.ValueFor(StableUK.card) is not { } cardId)
			{
				this.OnMouseDown(g, b);
				return;
			}

			if (cards.FirstOrDefault(card => card.uuid == cardId) is not { } tinkerCard)
				return;
			if (g.state.route is not Combat combat)
				return;

			if (tinkerCard.GetCurrentCost(g.state) > combat.energy)
			{
				tinkerCard.shakeNoAnim = 1;
				combat.pulseEnergyBad = 0.5;
				Audio.Play(Event.ZeroEnergy);
				return;
			}
			
			combat.QueueImmediate(new PlayCardAction { Card = tinkerCard });
			g.CloseRoute(this);
		}
	}

	private sealed class PlayCardAction : CardAction
	{
		public required Card Card;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			timer = 0;

			var cost = Card.GetCurrentCost(MG.inst.g?.state ?? DB.fakeState);
			if (cost > c.energy)
			{
				Card.shakeNoAnim = 1;
				Audio.Play(Event.ZeroEnergy);
				return;
			}

			c.energy -= cost;
			c.QueueImmediate(ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeAction(Card).SetShowTheCardIfNotInHand(false).AsCardAction);
		}
	}
}