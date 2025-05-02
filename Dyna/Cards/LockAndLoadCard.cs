using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dyna;

internal sealed class LockAndLoadCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/LockAndLoad.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LockAndLoad", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, description = ModEntry.Instance.Localizations.Localize(["card", "LockAndLoad", "description", upgrade.ToString()]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAddCard { destination = CardDestination.Hand, card = new CustomChargeCard(), amount = 2 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
			],
			Upgrade.B => [
				new AAddCard { destination = CardDestination.Hand, card = new CustomChargeCard { upgrade = Upgrade.B }, amount = 1 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
			_ => [
				new AAddCard { destination = CardDestination.Hand, card = new CustomChargeCard(), amount = 2 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			]
		};
}
