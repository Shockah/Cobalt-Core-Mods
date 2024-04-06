using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class IncomingCard : Card, IRegisterable
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
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Incoming.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Incoming", "name"]).Localize
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
			new AMove
			{
				targetPlayer = true,
				dir = -2
			},
			new AAttack
			{
				damage = GetDmg(s, 2)
			}.SetBlastwave(
				damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, upgrade == Upgrade.B ? 1 : 0)
			)
		];
}
