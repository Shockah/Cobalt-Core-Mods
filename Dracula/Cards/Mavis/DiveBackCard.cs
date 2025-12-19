using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class DiveBackCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("DiveBack", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Dodge,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "DiveBack", "name"]).Localize
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
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status), xHint = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 0 },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status) + 1 + s.ship.Get(Status.boost), xHint = 1 }, // TODO: Rosa compat?
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 0 },
			],
			_ => [
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status), xHint = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 0 },
			]
		};
}
