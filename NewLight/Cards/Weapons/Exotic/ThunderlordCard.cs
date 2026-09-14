using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class ThunderlordCard : ExoticWeaponCard, IHasCustomCardTraits, IRegisterable
{
	protected override WeaponElement WeaponElement => WeaponElement.Arc;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "Thunderlord", "Name"]).Localize,
		});
		
		Ammo.SetBaseHeavyCost(entry.UniqueName, 1);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 3);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry>
		{
			FullAutoCardTrait.Trait,
			ModEntry.Instance.KokoroApi.Finite.Trait,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 0), stunEnemy = true, fast = true },
				new ScatterAction { damage = GetDmg(s, 3) },
			],
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 1), fast = true },
				new ScatterAction { damage = GetDmg(s, 4) },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1), fast = true },
				new ScatterAction { damage = GetDmg(s, 3) },
			],
		};
}