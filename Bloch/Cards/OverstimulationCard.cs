using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class OverstimulationCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Overstimulation.png"), StableSpr.cards_Vamoose).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Overstimulation", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 2,
				Upgrade.B => 1,
				_ => 3
			},
			exhaust = upgrade == Upgrade.B,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = Status.evade,
					statusAmount = 2
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.KokoroApi.OxidationStatus.Status,
					statusAmount = 2
				}).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = Status.evade,
					statusAmount = 2
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.KokoroApi.OxidationStatus.Status,
					statusAmount = 3
				}).AsCardAction,
				new AStatus
				{
					targetPlayer = true,
					mode = AStatusMode.Set,
					status = ModEntry.Instance.KokoroApi.OxidationStatus.Status,
					statusAmount = 0
				}
			]
		};
}
