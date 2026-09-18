using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class VortexSwordCard : LegendaryWeaponCard, WeaponCard.IUsesAmmo, WeaponCard.ICannotCrit, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Arc, "Night Terror" },
		{ WeaponElement.Solar, "False Idols" },
		{ WeaponElement.Void, "Falling Guillotine" },
		{ WeaponElement.Stasis, "The Slammer" },
		{ WeaponElement.Strand, "Thin Precipice" },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "VortexSword"]).Localize,
		});
		
		Ammo.SetBaseHeavyCost(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1, flippable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new VolleyAction { damage = GetDmg(s, 5), VolleyDirection = flipped ? -2 : 2 },
			new AMove { targetPlayer = true, dir = 3 },
		];
}