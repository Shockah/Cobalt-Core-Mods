using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class HighImpactSniperRifleCard : LegendaryWeaponCard, WeaponCard.IUsesAmmo, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "Succession" },
		{ WeaponElement.Arc, "Occluded Finality" },
		{ WeaponElement.Solar, "Last Foray" },
		{ WeaponElement.Void, "Frozen Orbit" },
		{ WeaponElement.Stasis, "Critical Anomaly" },
		{ WeaponElement.Strand, "Volta Bracket" },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "HighImpactSniperRifle"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 2);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, 3);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 3 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, 5), piercing = true },
		];
}