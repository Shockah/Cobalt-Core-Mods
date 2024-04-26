using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class StrategizeCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Strategize.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Strategize", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Strategize", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		if (upgrade == Upgrade.B)
		{
			actions.Add(new AAddCard
			{
				card = new LeverageCard(),
				destination = CardDestination.Discard
			});
			actions.Add(new AAddCard
			{
				card = new BrainstormCard(),
				destination = CardDestination.Discard
			});
		}
		else
		{
			actions.Add(new ASpecificCardOffering
			{
				Destination = CardDestination.Deck,
				Cards = [
					new LeverageCard(),
					new BrainstormCard(),
				],
			});
			actions.Add(new ATooltipAction
			{
				Tooltips = [
					new TTCard { card = new LeverageCard() },
					new TTCard { card = new BrainstormCard() },
				]
			});
		}
		return actions;
	}
}
