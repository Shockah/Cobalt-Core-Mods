using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

// TODO: these should technically attack from the cannon, and then spread to two more tiles. similar to Blast

internal class WaveGrenadeLauncherCard : LegendaryWeaponCard, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Arc, "Forbearance" },
		{ WeaponElement.Solar, "Explosive Personality" },
		{ WeaponElement.Void, "Romantic Death" },
		{ WeaponElement.Stasis, "New Pacific Epitaph" },
		{ WeaponElement.Strand, "Tusk of the Boar" },
	};

	protected override List<string> AllowedPerkUniqueNames { get; } = [
		.. GlobalAllowedPerkUniqueNames.Value,
		ModEntry.Instance.Helper.Content.Cards.RetainCardTrait.UniqueName,
		ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait.UniqueName,
		ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait.UniqueName,
		AutoLoadingHolsterWeaponPerk.Trait.UniqueName,
		EnviousArsenalWeaponPerk.Trait.UniqueName,
		QuickdrawWeaponPerk.Trait.UniqueName,
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "WaveGrenadeLauncher"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 0, flippable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new WaveAction { damage = GetDmg(s, 3), Direction = flipped ? -2 : 2 },
		];
}