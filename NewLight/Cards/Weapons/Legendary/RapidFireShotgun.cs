using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class RapidFireShotgunCard : LegendaryWeaponCard, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "Perfect Paradox" },
		{ WeaponElement.Arc, "The Deicide" },
		{ WeaponElement.Solar, "Seventh Seraph CQC-12" },
		{ WeaponElement.Void, "Basso Ostinato" },
		{ WeaponElement.Stasis, "One Small Step" },
		{ WeaponElement.Strand, "Until Its Return" },
	};

	protected override List<string> AllowedPerkUniqueNames { get; } = [
		.. GlobalAllowedPerkUniqueNames.Value,
		ModEntry.Instance.Helper.Content.Cards.RetainCardTrait.UniqueName,
		ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait.UniqueName,
		AutoLoadingHolsterWeaponPerk.Trait.UniqueName,
		EnviousArsenalWeaponPerk.Trait.UniqueName,
		QuickdrawWeaponPerk.Trait.UniqueName,
		RimestealerWeaponPerk.Trait.UniqueName,
	];

	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Weapon.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "RapidFireShotgun"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 1);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1, flippable = true };

	public override IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
	{
		var baseResults = base.GetInnateTraits(state);
		var results = (baseResults as HashSet<ICardTraitEntry>) ?? new HashSet<ICardTraitEntry>(baseResults);
		results.Add(ModEntry.Instance.KokoroApi.Finite.Trait);
		return results;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AMove { targetPlayer = true, dir = 1 },
			new ScatterAction { damage = GetDmg(s, 2), Direction = flipped ? 1 : -1 },
		];
}