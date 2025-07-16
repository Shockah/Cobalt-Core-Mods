using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class PrepareResourcesCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PrepareResources.png"), StableSpr.cards_BubbleField).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PrepareResources", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = CramManager.CramStatus.Status, statusAmount = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = CramManager.CramStatus.Status, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = CramManager.CramStatus.Status, statusAmount = 1 },
			],
		};
}