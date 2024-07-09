using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class VoltageTuningCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/VoltageTuning.png"), StableSpr.cards_Overclock).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "VoltageTuning", "name"]).Localize
		});

		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.B, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.A => [],
			_ => [Limited.Trait],
		});

	public override CardData GetData(State state)
		=> new() { cost = 1, floppable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = 2, disabled = flipped },
				new ChangeLimitedUsesAction { CardId = uuid, Amount = -2, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = -2, disabled = !flipped },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = -1, disabled = !flipped },
			],
			_ => [
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = 1, disabled = flipped },
				new ChangeLimitedUsesAction { CardId = uuid, Amount = -2, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = -1, disabled = !flipped },
			]
		};
}
