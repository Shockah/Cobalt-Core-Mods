using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class BulletPointCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Special/BulletPoint.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BulletPoint", "name"]).Localize
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
	{
		List<CardAction> actions = [];
		if (upgrade == Upgrade.B)
			actions.Add(new AStatus
			{
				targetPlayer = true,
				status = Status.tempShield,
				statusAmount = 2
			});
		actions.Add(new AAttack
		{
			damage = GetDmg(s, upgrade == Upgrade.A ? 3 : 2),
			piercing = true
		});
		return actions;
	}
}
