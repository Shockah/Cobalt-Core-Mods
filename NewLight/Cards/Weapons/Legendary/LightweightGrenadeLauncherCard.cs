using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class LightweightGrenadeLauncherCard : LegendaryWeaponCard, WeaponCard.IUsesAmmo, WeaponCard.ICannotCrit, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "Mountaintop" },
		{ WeaponElement.Arc, "Salvager's Salvo" },
		{ WeaponElement.Solar, "Empty Vessel" },
		{ WeaponElement.Void, "Wilderflight" },
		{ WeaponElement.Stasis, "Lingering Dread" },
		{ WeaponElement.Strand, "Gizmo Weft" },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "LightweightGrenadeLauncher"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new BlastAction { damage = GetDmg(s, 4) },
		];
}