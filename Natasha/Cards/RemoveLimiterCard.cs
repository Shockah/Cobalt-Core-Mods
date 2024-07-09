using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class RemoveLimiterCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/RemoveLimiter.png"), StableSpr.cards_Ace).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RemoveLimiter", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
				new StepAction { CardId = uuid, Step = 1, Steps = 2, Action = new AAddCard { card = new LimiterCard(), destination = CardDestination.Deck } },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 3 },
				new TimesPlayedVariableHint { CardId = uuid },
				new AAddCard { card = new LimiterCard(), destination = CardDestination.Deck, amount = this.GetTimesPlayed() + 1, xHint = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
				new AAddCard { card = new LimiterCard(), destination = CardDestination.Deck },
			]
		};
}
