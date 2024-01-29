using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class BurnOutCard : Card, IRegisterable
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
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Special/BurnOut.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BurnOut", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			temporary = true,
			exhaust = true,
			retain = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [
			new AEnergy
			{
				changeAmount = upgrade == Upgrade.A ? 3 : 2
			}
		];
		if (upgrade != Upgrade.B)
			actions.Add(new AStatus
			{
				targetPlayer = true,
				status = Status.energyLessNextTurn,
				statusAmount = 1
			});
		return actions;
	}
}
