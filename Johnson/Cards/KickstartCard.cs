using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class KickstartCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Kickstart.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Kickstart", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 0,
			buoyant = true,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Kickstart", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		actions.Add(new ACardSelect
		{
			browseSource = ModEntry.UpgradableCardsAnywhereBrowseSource,
			browseAction = new TemporarilyUpgradeBrowseAction
			{
				Discount = upgrade == Upgrade.A ? -1 : 0,
				Strengthen = upgrade == Upgrade.B ? 1 : 0
			},
			omitFromTooltips = true
		});
		if (upgrade == Upgrade.A)
			actions.Add(new ATooltipAction
			{
				Tooltips = [
					new TTGlossary("cardtrait.discount", 1)
				]
			});
		if (upgrade == Upgrade.B)
			actions.Add(new ATooltipAction
			{
				Tooltips = [
					ModEntry.Instance.Api.GetStrengthenTooltip(1)
				]
			});
		actions.Add(new ATooltipAction
		{
			Tooltips = [
				ModEntry.Instance.Api.TemporaryUpgradeTooltip
			]
		});
		return actions;
	}
}
