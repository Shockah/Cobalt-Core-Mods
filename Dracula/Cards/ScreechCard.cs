using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class ScreechCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Screech", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Screech.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Screech", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			retain = true,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = false,
					status = Status.overdrive,
					statusAmount = -1,
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 2,
				},
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = 1,
				},
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = false,
					status = Status.overdrive,
					statusAmount = -2,
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1,
				},
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = 2,
				},
			],
			_ => [
				new AStatus
				{
					targetPlayer = false,
					status = Status.overdrive,
					statusAmount = -1,
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1,
				},
			]
		};
}
