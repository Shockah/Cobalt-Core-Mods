using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class NumberCruncherCard : Card, IRegisterable
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
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/NumberCruncher.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "NumberCruncher", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			exhaust = upgrade != Upgrade.B,
			retain = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, upgrade == Upgrade.A ? 2 : 1), piercing = true },
			new AStatus { targetPlayer = true, status = Status.temporaryCheap, statusAmount = upgrade == Upgrade.A ? 2 : 1 }
		];
}
