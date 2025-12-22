using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class DeepBiteCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("DeepBite", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_DrakeCannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "DeepBite", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 2 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.A => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait },
			_ => new HashSet<ICardTraitEntry>(),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new MavisSwoopAction { Attack = new() { damage = 3, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 4 } },
			],
			_ => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 },
				new MavisSwoopAction { Attack = new() { damage = 1, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 4 } },
			]
		};
}