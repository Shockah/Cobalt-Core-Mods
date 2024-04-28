using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

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
		=> new()
		{
			cost = 2,
			description = ModEntry.Instance.Localizations.Localize(["card", "LockAndLoad", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAddCard
			{
				destination = CardDestination.Hand,
				card = new CustomChargeCard
				{
					upgrade = upgrade == Upgrade.A ? Upgrade.A : Upgrade.None
				},
				amount = 2
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.evade,
				statusAmount = upgrade == Upgrade.B ? 2 : 1
			}
		];
}
