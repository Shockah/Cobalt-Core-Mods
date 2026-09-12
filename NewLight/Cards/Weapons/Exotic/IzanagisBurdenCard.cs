using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class IzanagisBurdenCard : ExoticWeaponCard, IRegisterable
{
	protected override WeaponElement WeaponElement => WeaponElement.Kinetic;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "IzanagisBurden", "Name"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, Upgrade.B, 2);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, Upgrade.None, 4);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, Upgrade.A, 4);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, Upgrade.B, 5);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 3, floppable = true },
			_ => base.GetData(state) with { cost = 3 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AVariableHint { status = Ammo.SpecialStatus.Status, disabled = flipped },
				new AAttack { damage = GetDmg(s, (s.ship.Get(Ammo.SpecialStatus.Status) - 2) * 3), piercing = true, xHint = 3, disabled = flipped },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Ammo.SpecialStatus.Status, statusAmount = 0, disabled = flipped },
				new ADummyAction(),
				new AAttack { damage = GetDmg(s, 4), piercing = true, disabled = !flipped },
			],
			Upgrade.A => [
				new AVariableHint { status = Ammo.SpecialStatus.Status, disabled = flipped },
				new AAttack { damage = GetDmg(s, s.ship.Get(Ammo.SpecialStatus.Status) * 2), piercing = true, xHint = 2, disabled = flipped },
				new AStatus { targetPlayer = true, status = Ammo.SpecialStatus.Status, statusAmount = -2, disabled = flipped },
			],
			_ => [
				new AVariableHint { status = Ammo.SpecialStatus.Status, disabled = flipped },
				new AAttack { damage = GetDmg(s, s.ship.Get(Ammo.SpecialStatus.Status) * 2), piercing = true, xHint = 2, disabled = flipped },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Ammo.SpecialStatus.Status, statusAmount = 0, disabled = flipped },
			],
		};
}