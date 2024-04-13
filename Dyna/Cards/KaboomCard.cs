using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class KaboomCard : Card, IRegisterable
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
			Art = StableSpr.cards_Cannon,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Kaboom.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Kaboom", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 2 : 1
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack
				{
					damage = GetDmg(s, 1)
				}.SetBlastwave(
					damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 1)
				)
			],
			Upgrade.B => [
				new AAttack
				{
					damage = GetDmg(s, 2)
				}.SetBlastwave(
					damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 2)
				),
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyLessNextTurn,
					statusAmount = 1
				}
			],
			_ => [
				new AAttack
				{
					damage = GetDmg(s, 1)
				}.SetBlastwave(
					damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 1)
				),
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyLessNextTurn,
					statusAmount = 1
				}
			]
		};
}
