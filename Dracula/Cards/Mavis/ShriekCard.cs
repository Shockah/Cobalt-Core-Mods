using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class ShriekCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Shriek", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_FumeCannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Shriek", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, flippable = true, exhaust = true },
			_ => new() { cost = 1 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new MavisSwoopAction { Attack = new() { damage = 1, stunEnemy = true } },
				new MavisSwoopAction { Attack = new() { damage = 1, stunEnemy = true }, Direction = flipped ? -2 : 2 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new MavisSwoopAction { Attack = new() { damage = 1, stunEnemy = true } },
			],
			_ => [
				new MavisSwoopAction { Attack = new() { damage = 1, stunEnemy = true } },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
			]
		};
}