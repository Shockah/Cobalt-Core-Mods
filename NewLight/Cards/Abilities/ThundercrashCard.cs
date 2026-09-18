using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class ThundercrashCard : GuardianCard, IRegisterable
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "Thundercrash", "Name"]).Localize,
		});
		
		Abilities.SetAbilityType(entry.UniqueName, Abilities.SUPER_ABILITY);
		Abilities.SetBaseCooldown(entry.UniqueName, 15);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 2, flippable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove { targetPlayer = true, dir = 3, isRandom = true },
				new AAttack { damage = GetDmg(s, 12), BlastRange = 2 },
				new AMove { targetPlayer = true, dir = 2 },
			],
			Upgrade.A => [
				new AMove { targetPlayer = true, dir = 3 },
				new AAttack { damage = GetDmg(s, 10), BlastRange = 2 },
				new AMove { targetPlayer = true, dir = 1, isRandom = true },
			],
			_ => [
				new AMove { targetPlayer = true, dir = 5 },
				new AAttack { damage = GetDmg(s, 10), BlastRange = 2 },
				new AMove { targetPlayer = true, dir = 2, isRandom = true },
			],
		};
}