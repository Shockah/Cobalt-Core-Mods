using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class SonarCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Sonar", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Inverter,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Sonar", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 0, floppable = true, exhaust = true },
			_ => new() { cost = 1, floppable = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, disabled = flipped },
				new ADrawCard { count = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status), xHint = 1, disabled = flipped },
				new ADummyAction(),
				new ADrawCard { count = 3, disabled = !flipped }
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1, disabled = flipped },
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, disabled = flipped },
				new ADrawCard { count = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status) + 1 + s.ship.Get(Status.boost), xHint = 1, disabled = flipped }, // TODO: Rosa compat?
				new ADummyAction(),
				new ADrawCard { count = 3, disabled = !flipped }
			],
			_ => [
				new AVariableHint { status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, disabled = flipped },
				new ADrawCard { count = s.ship.Get(ModEntry.Instance.MavisCharacter.MissingStatus.Status), xHint = 1, disabled = flipped },
				new ADummyAction(),
				new ADrawCard { count = 2, disabled = !flipped }
			]
		};
}
