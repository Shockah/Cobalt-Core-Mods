using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class SafetyProtocolCard : Card, IRegisterable, IHasCustomCardTraits
{
	private int GetDataReentry;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SafetyProtocol.png"), StableSpr.cards_Inverter).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SafetyProtocol", "name"]).Localize,
		});

		helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (e.Card is not SafetyProtocolCard card)
				return;

			card.GetDataReentry++;
			try
			{
				var unplayable = card.GetDataReentry == 1 && e.State.route is Combat { routeOverride: null } combat && (
					card.flipped
						? combat.energy < card.GetCurrentCost(MG.inst.g?.state ?? DB.fakeState) + 1
						: combat.hand.All(someCard => !someCard.IsAnalyzable(MG.inst.g?.state ?? DB.fakeState, combat))
				);
				
				if (unplayable)
					e.SetOverride(ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait, true);
			}
			finally
			{
				card.GetDataReentry--;
			}
		};
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.B, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.A => [],
			_ => [ModEntry.Instance.KokoroApi.Finite.Trait],
		});

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
	{
		return upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction
				{
					CardId = uuid,
					Action = new AEnergy { changeAmount = 1 },
				},
				new SmartShieldAction { Amount = 1 },
			],
			a: () => [
				new AnalyzeCostAction
				{
					CardId = uuid,
					Action = new AEnergy { changeAmount = 1 },
				},
				new SmartShieldAction { Amount = 2 },
			],
			b: () => [
				new AnalyzeCostAction
				{
					CardId = uuid,
					Action = new AEnergy { changeAmount = 1 },
				},
				new SmartShieldAction { Amount = 1 },
				new ADrawCard { count = 1 },
			]
		);
	}
}