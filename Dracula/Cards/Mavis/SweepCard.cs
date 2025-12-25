using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class SweepCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Sweep", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_ScootRight,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Sweep", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, flippable = true, art = StableSpr.cards_Dodge },
			_ => new() { cost = 1, flippable = true, art = flipped ? StableSpr.cards_ScootLeft : StableSpr.cards_ScootRight },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new MavisSwoopAction { Attack = new() { damage = 1 }, Direction = flipped ? -2 : 2 },
				new MavisSwoopAction { Attack = new() { damage = 1 }, Direction = flipped ? 2 : -2 },
			],
			Upgrade.A => [
				new MavisSwoopAction { Attack = new() { damage = 1 }, Direction = flipped ? -3 : 3 },
			],
			_ => [
				new MavisSwoopAction { Attack = new() { damage = 1 }, Direction = flipped ? -2 : 2 },
			]
		};
}