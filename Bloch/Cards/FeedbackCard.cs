using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class FeedbackCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Inverter,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Feedback.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Feedback", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 2 : 1,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				}
			],
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 2
				}
			]
		};
}
