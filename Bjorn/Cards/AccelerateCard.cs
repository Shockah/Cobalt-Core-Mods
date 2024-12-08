using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class AccelerateCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Accelerate.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Accelerate", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Accelerate", "description", upgrade.ToString()]);
		return upgrade.Switch<CardData>(
			() => new() { cost = 1, exhaust = true, description = description },
			() => new() { cost = 1, description = description },
			() => new() { cost = 3, exhaust = true, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [new AnalyzeCostAction { FilterAccelerated = false, FilterMinCost = 2, FilterExhaust = false, Action = new Action { Permanent = false } }],
			a: () => [new AnalyzeCostAction { FilterAccelerated = false, FilterMinCost = 2, FilterExhaust = false, Action = new Action { Permanent = false } }],
			b: () => [new AnalyzeCostAction { Permanent = true, FilterAccelerated = false, FilterMinCost = 2, FilterExhaust = false, Action = new Action { Permanent = true } }]
		);

	private sealed class Action : CardAction
	{
		public required bool Permanent;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. AcceleratedManager.Trait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, AcceleratedManager.Trait, true, permanent: Permanent);
		}
	}
}
