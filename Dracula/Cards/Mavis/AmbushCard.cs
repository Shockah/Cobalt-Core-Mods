using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class AmbushCard : Card, IDraculaCard,  IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Ambush", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Ambush", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.A => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait },
			_ => new HashSet<ICardTraitEntry>(),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = false },
					new AAttack { damage = GetDmg(s, 3) }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = true },
					new AAttack { damage = GetDmg(s, 1) }
				).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = false },
					new AAttack { damage = GetDmg(s, 3), piercing = true }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = true },
					new AAttack { damage = GetDmg(s, 1) }
				).AsCardAction,
			]
		};
}
