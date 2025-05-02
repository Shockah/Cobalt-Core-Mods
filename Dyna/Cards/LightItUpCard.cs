using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class LightItUpCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/LightItUp.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LightItUp", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, exhaust = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = NitroManager.TempNitroStatus.Status, statusAmount = 2 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = NitroManager.TempNitroStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.KokoroApi.StatusNextTurn.TempShield, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = NitroManager.TempNitroStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			]
		};
}
