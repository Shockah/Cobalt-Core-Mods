using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class SayThoughtsLoudCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SayThoughtsLoud.png"), StableSpr.cards_Serenity).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SayThoughtsLoud", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.drawLessNextTurn,
					statusAmount = 1,
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.boost,
					statusAmount = 2,
				},
				new AStatus
				{
					targetPlayer = false,
					status = Status.boost,
					statusAmount = -2,
				}
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.boost,
					statusAmount = 1,
				},
				new AStatus
				{
					targetPlayer = false,
					status = Status.boost,
					statusAmount = 2,
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.boost,
					statusAmount = 1,
				},
				new AStatus
				{
					targetPlayer = false,
					status = Status.boost,
					statusAmount = -1,
				}
			]
		};
}
