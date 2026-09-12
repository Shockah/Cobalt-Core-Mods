using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class FafnirCard : ExoticWeaponCard, IRegisterable
{
	protected override WeaponElement WeaponElement => WeaponElement.Void;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "Fafnir", "Name"]).Localize,
		});
		
		Ammo.SetBaseHeavyCost(entry.UniqueName, 2);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, 4);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 2, floppable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 3), weaken = true, disabled = flipped },
				new ADummyAction(),
				new AVariableHint { status = Ammo.HeavyStatus.Status, disabled = !flipped },
				new AAttack { damage = GetDmg(s, (s.ship.Get(Ammo.HeavyStatus.Status) - 2) * 3), xHint = 3, disabled = !flipped },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Ammo.HeavyStatus.Status, statusAmount = 0, disabled = !flipped },
			],
			Upgrade.A => [
				new AVariableHint { status = Ammo.HeavyStatus.Status, disabled = flipped },
				new AAttack { damage = GetDmg(s, (s.ship.Get(Ammo.HeavyStatus.Status) - 2) * 2), brittle = true, xHint = 2, disabled = flipped },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Ammo.HeavyStatus.Status, statusAmount = 0, disabled = flipped },
				new ADummyAction(),
				new AAttack { damage = GetDmg(s, 5), disabled = !flipped },
			],
			_ => [
				new AVariableHint { status = Ammo.HeavyStatus.Status, disabled = flipped },
				new AAttack { damage = GetDmg(s, (s.ship.Get(Ammo.HeavyStatus.Status) - 2) * 2), weaken = true, xHint = 2, disabled = flipped },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Ammo.HeavyStatus.Status, statusAmount = 0, disabled = flipped },
				new ADummyAction(),
				new AAttack { damage = GetDmg(s, 5), disabled = !flipped },
			],
		};
}