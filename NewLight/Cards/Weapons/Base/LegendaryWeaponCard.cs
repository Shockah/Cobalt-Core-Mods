using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal abstract class LegendaryWeaponCard : WeaponCard, IHasCustomCardTraits, IRegisterable
{
	protected const Rarity RARITY = Rarity.common;
	
	protected static readonly Lazy<List<string>> GlobalAllowedPerkUniqueNames = new(() => [
		FrenzyWeaponPerk.Trait.UniqueName,
		HeadstoneWeaponPerk.Trait.UniqueName,
		HealClipWeaponPerk.Trait.UniqueName,
		RepulsorBraceWeaponPerk.Trait.UniqueName,
		SurroundedWeaponPerk.Trait.UniqueName,
		VorpalWeaponWeaponPerk.Trait.UniqueName,
	]);

	private static readonly Dictionary<WeaponElement, ISpriteEntry> ElementCardFrames = [];
	internal static readonly Dictionary<string, WeaponElement> WeaponPerkElementAssignments = [];
	internal static readonly HashSet<string> DamageWeaponPerks = [];
	
	[JsonProperty] private string? BasePerkUniqueName, APerkUniqueName, BPerkUniqueName;
	[JsonProperty] private WeaponElement? WeaponElement;

	protected virtual List<string> AllowedPerkUniqueNames => GlobalAllowedPerkUniqueNames.Value;

	protected abstract Dictionary<WeaponElement, string> ElementWeaponNames { get; }

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		foreach (var element in Enum.GetValues<WeaponElement>())
			ElementCardFrames[element] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/CardFrames/LegendaryWeapons/{Enum.GetName(element)}.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetLocName)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetLocName_Postfix))
		);
	}

	[MemberNotNull(nameof(BasePerkUniqueName), nameof(APerkUniqueName), nameof(BPerkUniqueName), nameof(WeaponElement))]
	private void InitializeIfNeeded(State state)
	{
		if (WeaponElement is not null && BasePerkUniqueName is not null && APerkUniqueName is not null && BPerkUniqueName is not null)
			return;

		WeaponElement = ElementWeaponNames.Keys.Skip(state.rngCardOfferings.NextInt() % ElementWeaponNames.Count).First();
		
		var perks = AllowedPerkUniqueNames
			.Where(name => !WeaponPerkElementAssignments.TryGetValue(name, out var element) || element == WeaponElement)
			.ToList();
		
		var perkIndex = state.rngCardOfferings.NextInt() % perks.Count;
		BasePerkUniqueName = perks[perkIndex];
		perks.RemoveAt(perkIndex);

		if (DamageWeaponPerks.Contains(BasePerkUniqueName))
			perks = perks.Where(perk => !DamageWeaponPerks.Contains(perk)).ToList();
		
		perkIndex = state.rngCardOfferings.NextInt() % perks.Count;
		APerkUniqueName = perks[perkIndex];
		perks.RemoveAt(perkIndex);

		if (DamageWeaponPerks.Contains(APerkUniqueName))
			perks = perks.Where(perk => !DamageWeaponPerks.Contains(perk)).ToList();
		
		perkIndex = state.rngCardOfferings.NextInt() % perks.Count;
		BPerkUniqueName = perks[perkIndex];
		perks.RemoveAt(perkIndex);
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
		
		if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(BasePerkUniqueName) is { } basePerk)
			results.Add(basePerk);

		switch (upgrade)
		{
			case Upgrade.A:
				if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(APerkUniqueName) is { } aPerk)
					results.Add(aPerk);
				break;
			case Upgrade.B:
				if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(BPerkUniqueName) is { } bPerk)
					results.Add(bPerk);
				break;
		}
		
		return results;
	}

	private static void Card_GetLocName_Postfix(Card __instance, ref string __result)
	{
		if (__instance is not LegendaryWeaponCard legendary)
			return;

		if (legendary.WeaponElement is null)
		{
			if (MG.inst.g?.state is { } state)
				legendary.InitializeIfNeeded(state);
			else
				return;
		}

		__result = legendary.ElementWeaponNames[legendary.WeaponElement.Value];
	}

	internal Spr OverrideCardFrame(DeckConfiguration.CardFrameOverrideArgs args)
	{
		var state = args.State == DB.fakeState ? MG.inst.g.state : args.State;
		InitializeIfNeeded(state);
		return ElementCardFrames[WeaponElement.Value].Sprite;
	}
}