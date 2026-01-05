using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtBasicOverdriveCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Card/BasicOverdrive.png"), StableSpr.cards_Overdrive).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "card", "BasicOverdrive", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, temporary = true, exhaust = true, artTint = "ff3366" },
			Upgrade.A => new() { cost = 0, temporary = true, artTint = "ff3366" },
			_ => new() { cost = 1, temporary = true, artTint = "ff3366" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.overdrive, statusAmount = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.overdrive, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.overdrive, statusAmount = 1 },
			],
		};
}