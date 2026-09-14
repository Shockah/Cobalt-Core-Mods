using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class TraceRifleCard : LegendaryWeaponCard, WeaponCard.IUsesAmmo.IOnlyOne, WeaponCard.IRepeatable, WeaponCard.IEnergyFree, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Arc, "Keraunios" },
		{ WeaponElement.Solar, "Retraced Path" },
		{ WeaponElement.Void, "Hollow Denial" },
		{ WeaponElement.Stasis, "Action Item" },
		{ WeaponElement.Strand, "Unsworn" },
	};

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "TraceRifle"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 1);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 5);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 0 };

	public override IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
	{
		var baseResults = base.GetInnateTraits(state);
		var results = (baseResults as HashSet<ICardTraitEntry>) ?? new HashSet<ICardTraitEntry>(baseResults);
		results.Add(ModEntry.Instance.KokoroApi.Finite.Trait);
		return results;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, 1), fast = true },
		];
}