using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class ComboAttackCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("ComboAttack", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_MultiShot,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "ComboAttack", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 1), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
				new MavisSwoopAction { Attack = new() { damage = 1, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 } },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new AAttack { damage = GetDmg(s, 1) },
				new MavisSwoopAction { Attack = new() { damage = 1 } },
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1) },
				new MavisSwoopAction { Attack = new() { damage = 1 } },
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			]
		};
}