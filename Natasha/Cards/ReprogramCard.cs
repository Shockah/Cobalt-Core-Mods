using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Natasha;

internal sealed class ReprogramCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Reprogram", "name"]).Localize
		});
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.A => [ModEntry.Instance.KokoroApi.Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			Upgrade.B => new() { cost = 1, exhaust = true, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			_ => new() { cost = 1, exhaust = true, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = false, status = Reprogram.ReprogrammedStatus.Status, statusAmount = 1, disabled = flipped },
			new SpecificUpgradeAddCardAction { Upgrade = upgrade, card = new DeprogramCard { upgrade = upgrade }, amount = 1, destination = CardDestination.Discard, disabled = flipped },
			new ADummyAction(),
			new ASpawn { thing = new RepairKit(), disabled = !flipped },
			new SpecificUpgradeAddCardAction { Upgrade = upgrade, card = new DeprogramCard { upgrade = upgrade }, amount = 1, destination = CardDestination.Discard, disabled = !flipped },
		];

	private sealed class SpecificUpgradeAddCardAction : AAddCard
	{
		public required Upgrade Upgrade;
		
		public override Icon? GetIcon(State s)
		{
			if (base.GetIcon(s) is not { } icon)
				return null;
			icon.path = Upgrade switch
			{
				Upgrade.A => ModEntry.Instance.AddCardAIcon.Sprite,
				Upgrade.B => ModEntry.Instance.AddCardBIcon.Sprite,
				_ => icon.path,
			};
			return icon;
		}
	}
}
