using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class PromoteCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Promote.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Promote", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.A ? 0 : 1,
			description = ModEntry.Instance.Localizations.Localize(["card", "Promote", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ADrawCard { count = 2 },
				new ADelay(),
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new TemporarilyUpgradeBrowseAction(),
					filterUpgrade = Upgrade.None,
				},
				new ATooltipAction { Tooltips = new TemporarilyUpgradeBrowseAction().GetTooltips(s) },
			],
			_ => [
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new TemporarilyUpgradeBrowseAction(),
					filterUpgrade = Upgrade.None,
				},
				new ATooltipAction { Tooltips = new TemporarilyUpgradeBrowseAction().GetTooltips(s) },
			]
		};
}
