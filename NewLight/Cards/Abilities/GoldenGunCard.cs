using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class GoldenGunCard : GuardianCard, IHasCustomCardTraits, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "GoldenGun", "Name"]).Localize,
		});
		
		Abilities.SetAbilityType(entry.UniqueName, Abilities.SUPER_ABILITY);
		Abilities.SetBaseCooldown(entry.UniqueName, 15);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, Upgrade.None, 1);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, Upgrade.A, 1);
		PrecisionCardTrait.SetPrecision(entry.UniqueName, Upgrade.B, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 3);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 4) },
				new AMove { targetPlayer = true, dir = 1, isRandom = true },
				new AMove { targetPlayer = true, dir = 2, isRandom = true },
			],
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 3), piercing = true },
				new AMove { targetPlayer = true, dir = 1, isRandom = true },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 3) },
				new AMove { targetPlayer = true, dir = 1, isRandom = true },
			],
		};
}