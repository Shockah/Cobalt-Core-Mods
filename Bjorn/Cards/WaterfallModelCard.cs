using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class WaterfallModelCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/WaterfallModel.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "WaterfallModel", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, unplayable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new OnAnalyzeAction { Action = new AStatus { targetPlayer = true, status = GadgetManager.GetCorrectStatus(s), statusAmount = upgrade.Switch(3, 4, 5) } },
			new OnAnalyzeAction { Action = new AStatus { targetPlayer = true, status = Status.lockdown, statusAmount = upgrade.Switch(2, 2, 3) } },
			new OnAnalyzeAction { Action = new ExhaustCardAction { CardId = uuid } },
		];
}
