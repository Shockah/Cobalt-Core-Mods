using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class MergerCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Merger.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Merger", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1
		};

	private int GetX(State state)
	{
		var x = state.ship.Get(Status.shield);
		if (ModEntry.Instance.TyAndSashaApi is { } api)
			x += state.ship.Get(api.XFactorStatus);
		return x;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AVariableHint
				{
					status = Status.shield,
				},
				new AStrengthen
				{
					CardId = uuid,
					Amount = GetX(s),
					xHint = 1
				},
				new AAttack
				{
					damage = GetDmg(s, 1 + GetX(s))
				},
				new AStatus
				{
					targetPlayer = true,
					mode = AStatusMode.Set,
					status = Status.shield,
					statusAmount = 0
				}
			],
			_ => [
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AVariableHint
				{
					status = Status.shield,
				},
				new AStrengthen
				{
					CardId = uuid,
					Amount = GetX(s),
					xHint = 1
				},
				new AStatus
				{
					targetPlayer = true,
					mode = AStatusMode.Set,
					status = Status.shield,
					statusAmount = upgrade == Upgrade.B ? 1 : 0
				}
			]
		};
}
