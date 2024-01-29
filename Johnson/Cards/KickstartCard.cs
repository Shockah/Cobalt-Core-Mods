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
			cost = upgrade == Upgrade.B ? 1 : 0,
			buoyant = true,
			exhaust = upgrade != Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "Kickstart", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		var cardCount = upgrade == Upgrade.A ? 2 : 1;
		for (var i = 0; i < cardCount; i++)
			actions.Add(new ACardSelect
			{
				browseSource = ModEntry.UpgradableCardsAnywhereBrowseSource,
				browseAction = new TemporarilyUpgradeBrowseAction(),
				omitFromTooltips = true
			});
		return actions;
	}
}
