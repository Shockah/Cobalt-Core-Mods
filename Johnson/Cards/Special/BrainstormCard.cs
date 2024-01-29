using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class BrainstormCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Special/Brainstorm.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Brainstorm", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			temporary = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ADrawCard
			{
				count = upgrade switch
				{
					Upgrade.A => 5,
					Upgrade.B => 3,
					_ => 2
				}
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.drawNextTurn,
				statusAmount = upgrade == Upgrade.B ? 3 : 2,
			}
		];
}
