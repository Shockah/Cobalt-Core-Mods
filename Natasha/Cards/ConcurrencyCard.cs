using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ConcurrencyCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Concurrency.png"), StableSpr.cards_MultiBlast).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Concurrency", "name"]).Localize
		});

		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.A, 4);
		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.B, 4);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Limited.Trait };

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, infinite = true, floppable = true },
			_ => new() { cost = 2 }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new LimitedUsesVariableHint().Disabled(flipped),
				new AAttack { damage = GetDmg(s, this.GetLimitedUses()), xHint = 1, disabled = flipped },
				new ADummyAction(),
				new TimesPlayedVariableHint().Disabled(!flipped),
				new AAttack { damage = GetDmg(s, this.GetTimesPlayed() + 1), xHint = 1, disabled = !flipped },
			],
			_ => [
				new LimitedUsesVariableHint(),
				new AAttack { damage = GetDmg(s, this.GetLimitedUses()), xHint = 1 },
				new TimesPlayedVariableHint(),
				new AAttack { damage = GetDmg(s, this.GetTimesPlayed() + 1), xHint = 1 },
			]
		};
}
