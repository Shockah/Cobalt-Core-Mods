using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class NitroCard : Card, IRegisterable
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
			Art = StableSpr.cards_Overpower,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Nitro.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Nitro", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 2,
				Upgrade.B => 4,
				_ => 3
			},
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = NitroManager.NitroStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.powerdrive,
					statusAmount = 1
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = NitroManager.NitroStatus.Status,
					statusAmount = 1
				}
			]
		};
}
