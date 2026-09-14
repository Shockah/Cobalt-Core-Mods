using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class RocketLauncherCard : LegendaryWeaponCard, WeaponCard.IUsesAmmo.IOverHalf, WeaponCard.ICannotCrit, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Arc, "The Hothead" },
		{ WeaponElement.Solar, "Hezen Vengeance" },
		{ WeaponElement.Void, "Tomorrow's Answer" },
		{ WeaponElement.Stasis, "The When and Where" },
		{ WeaponElement.Strand, "Cynosure" },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "RocketLauncher"]).Localize,
		});
		
		Ammo.SetBaseHeavyCost(entry.UniqueName, 3);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 2 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new BlastAction { damage = GetDmg(s, 10), Range = 2 },
		];
}