using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class IncomingCard : Card, IRegisterable
{
	private static ISpriteEntry NormalArt = null!;
	private static ISpriteEntry FlippedArt = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		NormalArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Incoming.png"));
		FlippedArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/IncomingFlipped.png"));
		
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Incoming", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			flippable = upgrade == Upgrade.A,
			art = (flipped ? FlippedArt : NormalArt).Sprite,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AMove { targetPlayer = true, dir = -2 },
			new AAttack
			{
				damage = GetDmg(s, 2)
			}.SetBlastwave(
				damage: ModEntry.Instance.Api.GetBlastwaveDamage(this, s, upgrade == Upgrade.B ? 1 : 0)
			)
		];
}
