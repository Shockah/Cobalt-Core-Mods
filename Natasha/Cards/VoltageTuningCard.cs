using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "VoltageTuning", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [],
			_ => [ModEntry.Instance.KokoroApi.Limited.Trait],
		});

	public override CardData GetData(State state)
		=> new() { cost = 1, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = 2, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = -2, disabled = !flipped },
			],
			_ => [
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = false, status = Status.boost, statusAmount = -1, disabled = !flipped },
			]
		};
}
