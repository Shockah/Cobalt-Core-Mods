using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class PlungeCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Plunge", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_HandCannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Plunge", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 2 },
			_ => new() { cost = 1 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status },
				new MavisSwoopAction { Attack = new() { damage = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status) + 1, xHint = 1 } },
			],
			Upgrade.A => [
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status },
				new MavisSwoopAction { Attack = new() { damage = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status), xHint = 1 } },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = -1 },
			],
			_ => [
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status },
				new MavisSwoopAction { Attack = new() { damage = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status), xHint = 1 } },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
			]
		};
}