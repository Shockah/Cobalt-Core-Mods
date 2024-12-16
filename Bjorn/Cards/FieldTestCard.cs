using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class FieldTestCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/FieldTest.png"), StableSpr.cards_AdminDeploy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "FieldTest", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "FieldTest", "description", upgrade.ToString()]);
		return upgrade.Switch<CardData>(
			() => new() { cost = 0, description = description },
			() => new() { cost = 0, description = description },
			() => new() { cost = 1, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction { FilterExhaust = false, Action = new ChooseCardInYourHandToPlayForFree() },
				new TooltipAction { Tooltips = [
					new TTGlossary("cardtrait.exhaust"),
					new TTGlossary("action.bypass"),
				] },
			],
			a: () => [
				new AnalyzeCostAction { Action = new ChooseCardInYourHandToPlayForFree() },
				new TooltipAction { Tooltips = [new TTGlossary("action.bypass")] },
			],
			b: () => [
				new ACardSelect { browseSource = CardBrowse.Source.Hand, browseAction = new ChooseCardInYourHandToPlayForFree() },
				new TooltipAction { Tooltips = [new TTGlossary("action.bypass")] },
			]
		);
}
