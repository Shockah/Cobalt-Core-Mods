using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class DeadlineCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(typeof(Quarter1Card)),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Quarters/Deadline.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Deadline", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.B ? 1 : 3,
			temporary = true,
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ADummyAction(),
			new AStatus
			{
				targetPlayer = true,
				status = Status.powerdrive,
				statusAmount = upgrade switch
				{
					Upgrade.A => 3,
					Upgrade.B => 1,
					_ => 2
				}
			},
			new ADummyAction() { dialogueSelector = $".Played::{Key()}" },
		];
}
