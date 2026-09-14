using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class WaveGrenadeLauncherCard : LegendaryWeaponCard, WeaponCard.IEnergyFree, WeaponCard.IUsesAmmo, WeaponCard.ICannotCrit, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Arc, "Forbearance" },
		{ WeaponElement.Solar, "Explosive Personality" },
		{ WeaponElement.Void, "Romantic Death" },
		{ WeaponElement.Stasis, "New Pacific Epitaph" },
		{ WeaponElement.Strand, "Tusk of the Boar" },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "WaveGrenadeLauncher"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 0, flippable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new BlastAction { damage = GetDmg(s, 3), Range = 2, Direction = flipped ? -2 : 2 },
		];
}