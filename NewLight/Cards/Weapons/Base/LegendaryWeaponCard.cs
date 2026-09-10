using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.NewLight;

internal abstract class LegendaryWeaponCard : Card, IHasCustomCardTraits
{
	protected const Rarity Rarity = global::Rarity.common;
	
	protected static readonly List<string> GlobalAllowedPerkUniqueNames = [
		FrenzyWeaponPerk.Trait.UniqueName,
		SurroundedWeaponPerk.Trait.UniqueName,
		VorpalWeaponWeaponPerk.Trait.UniqueName,
	];
	
	[JsonProperty] private string? BasePerkUniqueName, APerkUniqueName, BPerkUniqueName;

	protected virtual List<string> AllowedPerkUniqueNames => GlobalAllowedPerkUniqueNames;

	protected abstract Dictionary<WeaponElement, string> WeaponNames { get; }

	[MemberNotNull(nameof(BasePerkUniqueName), nameof(APerkUniqueName), nameof(BPerkUniqueName))]
	private void InitializeIfNeeded(State state)
	{
		if (BasePerkUniqueName is not null && APerkUniqueName is not null && BPerkUniqueName is not null)
			return;
		
		var perks = AllowedPerkUniqueNames.ToList();
		
		var perkIndex = state.rngCardOfferings.NextInt() % perks.Count;
		BasePerkUniqueName = perks[perkIndex];
		perks.RemoveAt(perkIndex);
		
		perkIndex = state.rngCardOfferings.NextInt() % perks.Count;
		APerkUniqueName = perks[perkIndex];
		perks.RemoveAt(perkIndex);
		
		perkIndex = state.rngCardOfferings.NextInt() % perks.Count;
		BPerkUniqueName = perks[perkIndex];
		perks.RemoveAt(perkIndex);
	}

	public ICardTraitEntry? ObtainBasePerk(State state)
	{
		InitializeIfNeeded(state);
		return ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(BasePerkUniqueName);
	}
	
	public ICardTraitEntry? ObtainAPerk(State state)
	{
		InitializeIfNeeded(state);
		return ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(APerkUniqueName);
	}
	
	public ICardTraitEntry? ObtainBPerk(State state)
	{
		InitializeIfNeeded(state);
		return ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(BPerkUniqueName);
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
		
		if (ObtainBasePerk(state) is { } basePerk)
			results.Add(basePerk);

		switch (upgrade)
		{
			case Upgrade.A:
				if (ObtainAPerk(state) is { } aPerk)
					results.Add(aPerk);
				break;
			case Upgrade.B:
				if (ObtainBPerk(state) is { } bPerk)
					results.Add(bPerk);
				break;
		}
		
		return results;
	}
}