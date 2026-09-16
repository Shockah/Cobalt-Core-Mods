using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal abstract class LegendaryWeaponCard : WeaponCard, IHasCustomCardTraits, IRegisterable
{
	public const Rarity RARITY = Rarity.common;

	internal static readonly Dictionary<string, Func<WeaponCard, bool>> WeaponPerkConditions = [];
	internal static readonly Dictionary<string, WeaponElement> WeaponPerkElementAssignments = [];
	internal static readonly HashSet<string> DamageWeaponPerks = [];

	private static HashSet<Card>? CachedReleasedCardSet;
	private static Dictionary<int, Card>? CachedReleasedCardIdMap;
	private static ICardTraitEntry RandomBaseWeaponPerkTrait = null!;
	private static ICardTraitEntry RandomUpgradedWeaponPerkTrait = null!;

	[JsonProperty("WeaponElement")] private WeaponElement? MaybeWeaponElement;
	[JsonProperty] private Dictionary<Upgrade, string> PerkUniqueNames = [];

	protected abstract Dictionary<WeaponElement, string> ElementWeaponNames { get; }

	protected override WeaponElement WeaponElement
		=> MaybeWeaponElement ?? WeaponElement.Kinetic;

	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var randomBaseWeaponPerkIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/RandomBaseWeaponPerk.png"));
		var randomUpgradedWeaponPerkIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/RandomUpgradedWeaponPerk.png"));
		
		// reversed order for tooltip ordering reasons
		RandomUpgradedWeaponPerkTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("RandomUpgradedWeaponPerk", new()
		{
			Icon = (_, _) => randomUpgradedWeaponPerkIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "RandomUpgradedWeaponPerk", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::RandomUpgradedWeaponPerk")
				{
					Icon = randomUpgradedWeaponPerkIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "RandomUpgradedWeaponPerk", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "RandomUpgradedWeaponPerk", "Description"]),
				}
			]
		});
		RandomBaseWeaponPerkTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("RandomBaseWeaponPerk", new()
		{
			Icon = (_, _) => randomBaseWeaponPerkIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "RandomBaseWeaponPerk", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::RandomBaseWeaponPerk")
				{
					Icon = randomBaseWeaponPerkIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "RandomBaseWeaponPerk", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "RandomBaseWeaponPerk", "Description"]),
				}
			]
		});

		WeaponPerkConditions[ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait.UniqueName] = _ => true;
		WeaponPerkConditions[ModEntry.Instance.Helper.Content.Cards.RetainCardTrait.UniqueName] = _ => true;
		WeaponPerkConditions[ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait.UniqueName] = weapon => weapon is not (IUsesAmmo and IRepeatable) && weapon is not IUsesAmmo.IOverHalf;
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetLocName)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetLocName_Postfix))
		);
	}

	private static bool IsDbCard(Card card)
	{
		if (CachedReleasedCardSet is not null && CachedReleasedCardSet.Contains(card))
			return true;
		if (CachedReleasedCardIdMap is not null)
			return CachedReleasedCardIdMap.TryGetValue(card.uuid, out var cachedCard) && cachedCard.GetType() == card.GetType();
		
		if (ModEntry.Instance.Helper.Events.ModLoadPhaseState is not { Phase: ModLoadPhase.AfterDbInit, IsDone: true })
		{
			var cardType = card.GetType();
			foreach (var releasedCard in DB.releasedCards)
			{
				if (releasedCard == card)
					return true;
				if (releasedCard.GetType() == cardType && releasedCard.uuid == card.uuid)
					return true;
			}
		}

		CachedReleasedCardSet = DB.releasedCards.ToHashSet();
		CachedReleasedCardIdMap = DB.releasedCards.ToDictionary(releasedCard => releasedCard.uuid, releasedCard => releasedCard);
		return CachedReleasedCardSet.Contains(card);
	}

	private void InitializeIfNeeded(State state)
	{
		if (MaybeWeaponElement is not null && PerkUniqueNames.Count != 0)
			return;
		if (IsDbCard(this))
			return;

		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (MG.inst.g.state is not null)
			state = MG.inst.g.state;

		MaybeWeaponElement = ElementWeaponNames.Keys.Skip(state.rngCardOfferings.NextInt() % ElementWeaponNames.Count).First();
		
		var perks = WeaponPerkConditions
			.Where(kvp => kvp.Value(this))
			.Select(kvp => kvp.Key)
			.Where(name => !WeaponPerkElementAssignments.TryGetValue(name, out var element) || element == WeaponElement)
			.ToList();

		var elementUniquePerks = perks
			.Where(name => WeaponPerkElementAssignments.TryGetValue(name, out var element) && element == WeaponElement)
			.ToList();
		
		var upgradesLeft = GetMeta().upgradesTo.Prepend(Upgrade.None).ToList();

		if (elementUniquePerks.Count != 0 && state.rngCardOfferings.Next() < 0.5)
		{
			var upgrade = upgradesLeft[state.rngCardOfferings.NextInt() % upgradesLeft.Count];
			var elementUniquePerk = elementUniquePerks[state.rngCardOfferings.NextInt() % elementUniquePerks.Count];
			PerkUniqueNames[upgrade] = elementUniquePerk;
			perks.Remove(elementUniquePerk);
			upgradesLeft.Remove(upgrade);
		}

		foreach (var upgrade in upgradesLeft)
		{
			IEnumerable<Upgrade> toCheck = upgrade == Upgrade.None ? PerkUniqueNames.Keys : [Upgrade.None];
			var hasDamagePerk = toCheck.Any(otherUpgrade => PerkUniqueNames.TryGetValue(otherUpgrade, out var perk) && DamageWeaponPerks.Contains(perk));
			var possiblePerks = hasDamagePerk ? perks.Where(perk => !DamageWeaponPerks.Contains(perk)).ToList() : perks;
			var perk = possiblePerks[state.rngCardOfferings.NextInt() % possiblePerks.Count];
			PerkUniqueNames[upgrade] = perk;
			perks.Remove(perk);
		}
	}

	public override CardData GetData(State state)
	{
		var innateTraits = GetInnateTraits(state);
		return new()
		{
			retain = innateTraits.Contains(ModEntry.Instance.Helper.Content.Cards.RetainCardTrait),
			recycle = innateTraits.Contains(ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait),
			buoyant = innateTraits.Contains(ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait),
		};
	}

	public virtual IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
	{
		var results = new HashSet<ICardTraitEntry>();
		InitializeIfNeeded(state);

		results.Add((PerkUniqueNames.TryGetValue(Upgrade.None, out var basePerkUniqueName) ? ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(basePerkUniqueName) : null) ?? RandomBaseWeaponPerkTrait);
		if (upgrade != Upgrade.None)
			results.Add((PerkUniqueNames.TryGetValue(upgrade, out var upgradedPerkUniqueName) ? ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(upgradedPerkUniqueName) : null) ?? RandomUpgradedWeaponPerkTrait);
		
		return results;
	}

	private static void Card_GetLocName_Postfix(Card __instance, ref string __result)
	{
		if (__instance is not LegendaryWeaponCard legendary)
			return;
		
		if (MG.inst.g?.state is { } state)
			legendary.InitializeIfNeeded(state);
		
		if (legendary.MaybeWeaponElement is not { } element)
			return;

		__result = legendary.ElementWeaponNames[element];
	}

	internal override Spr OverrideCardFrame(DeckConfiguration.CardFrameOverrideArgs args)
	{
		InitializeIfNeeded(args.State);
		return base.OverrideCardFrame(args);
	}
}