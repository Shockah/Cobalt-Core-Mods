using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class MegatonBlastCard : Card, IRegisterable
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
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/MegatonBlast.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MegatonBlast", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 3,
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack
				{
					damage = GetDmg(s, 4)
				}.SetBlastwave(
					damage: GetDmg(s, 2),
					range: 2
				),
				new AEndTurn()
			],
			Upgrade.B => [
				new AAttack
				{
					damage = GetDmg(s, 4)
				}.SetBlastwave(
					damage: GetDmg(s, 3),
					range: 2
				),
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyLessNextTurn,
					statusAmount = 2
				},
				new AEndTurn()
			],
			_ => [
				new AAttack
				{
					damage = GetDmg(s, 4)
				}.SetBlastwave(
					damage: GetDmg(s, 2),
					range: 2
				),
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyLessNextTurn,
					statusAmount = 2
				},
				new AEndTurn()
			]
		};
}
