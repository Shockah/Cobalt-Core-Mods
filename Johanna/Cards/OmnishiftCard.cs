using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Johanna;

internal sealed class OmnishiftCard : JohannaCard, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohannaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/Omnishift.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Omnishift", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 1, exhaust = true },
			_ => base.GetData(state) with { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove { targetPlayer = false, dir = 4 },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
			Upgrade.A => [
				new AMove { targetPlayer = false, dir = 1 },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
			_ => [
				new AMove { targetPlayer = false, dir = 1 },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
		};
}