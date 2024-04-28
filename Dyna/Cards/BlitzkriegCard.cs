using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BlitzkriegCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Blitzkrieg.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Blitzkrieg", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			flippable = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, upgrade == Upgrade.B ? 2 : 1)
			}.SetBlastwave(
				damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 1, blastwaveIndex: 0)
			),
			new AMove
			{
				targetPlayer = true,
				dir = 2,
				isRandom = upgrade != Upgrade.A
			},
			new AAttack
			{
				damage = GetDmg(s, upgrade == Upgrade.B ? 2 : 1)
			}.SetBlastwave(
				damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 1, blastwaveIndex: 1)
			),
			new AStatus
			{
				targetPlayer = true,
				status = Status.energyLessNextTurn,
				statusAmount = 1
			}
		];
}
