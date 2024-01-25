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
	{
		List<CardAction> actions = [];
		if (upgrade == Upgrade.B)
			actions.Add(new ADrawCard
			{
				count = 2
			});
		actions.Add(new ACardSelect
		{
			browseSource = CardBrowse.Source.Hand,
			browseAction = new TemporarilyUpgradeBrowseAction(),
			omitFromTooltips = true
		});
		return actions;
	}

	public sealed class TemporarilyUpgradeBrowseAction : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			if (selectedCard is null)
				return baseResult;

			selectedCard.SetTemporarilyUpgraded(true);
			return new CardUpgrade
			{
				cardCopy = Mutil.DeepCopy(selectedCard)
			};
		}
	}
}
