using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BangCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Bang.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Bang", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0 },
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 1) }.SetBlastwave(ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 0), range: 2),
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2) }.SetBlastwave(ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 0), range: 2),
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1) }.SetBlastwave(ModEntry.Instance.Api.GetBlastwaveDamage(this, s, 0), range: 2),
			]
		};
}
