using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class PrototypeCard : Card, IRegisterable, IHasCustomCardTraits
{
	public readonly List<Entry> Tinkers = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
				unreleased = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Prototype.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "name"]).Localize,
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetFullDisplayName)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetFullDisplayName_Prefix))
		);
	}

	public override CardData GetData(State state)
	{
		var data = upgrade.Switch<CardData>(
			none: () => new() { cost = 1, exhaust = true },
			a: () => new() { cost = 1, exhaust = true, retain = true },
			b: () => new() { cost = 1, exhaust = true }
		);
		
		foreach (var entry in Tinkers)
			entry.Tinker.ModifyCardData(state, this, entry.Level, ref data);

		return data;
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> Tinkers.SelectMany(t => t.Tinker.GetInnateTraits(state, this, t.Level)).ToHashSet();

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var actions = Tinkers.SelectMany(t => t.Tinker.GetActions(s, c, this, t.Level)).ToList();
		if (upgrade == Upgrade.B)
			actions.Add(new OnAnalyzeAction { Action = new TinkerAction { CardId = uuid } });
		return actions;
	}
	
	// TODO: handle run summary
	private static bool Card_GetFullDisplayName_Prefix(Card __instance, ref string __result)
	{
		if (__instance is not PrototypeCard card)
			return true;

		var name = card.GetLocName();
		var suffixes = card.Tinkers
			.Select(t => t.Tinker.GetCardNameSuffix(MG.inst.g?.state ?? DB.fakeState, card, t.Level))
			.Where(suffix => !string.IsNullOrEmpty(suffix))
			.ToList();

		if (suffixes.Count != 0)
			name = $"{name} {string.Concat(suffixes)}";
		
		__result = DB.Join(name, card.upgrade switch
		{
			Upgrade.None => "",
			Upgrade.A => " A",
			Upgrade.B => " B",
			_ => " ?",
		});
		
		return false;
	}

	public sealed class Entry
	{
		public required string TinkerUniqueName;
		public required ITinker Tinker;
		public int Level = 1;
	}
}