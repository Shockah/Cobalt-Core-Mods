using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class TerminateCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
				unreleased = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Terminate.png"), StableSpr.cards_ColorlessTrash).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Terminate", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 0, singleUse = true, retain = true },
			a: () => new() { cost = 0, singleUse = true, retain = true },
			b: () =>
			{
				var isLastUse = state.ship.Get(GadgetManager.GadgetStatus.Status) <= 1;
				return new()
				{
					cost = 0, retain = true,
					infinite = !isLastUse,
					singleUse = isLastUse,
				};
			}
		);

	// TODO: replace with an API, when that becomes available
	private static int GetXAmount(State state)
	{
		var amount = state.ship.Get(GadgetManager.GadgetStatus.Status);
		if (ModEntry.Instance.TyAndSashaApi is { } tyAndSashaApi)
			amount += state.ship.Get(tyAndSashaApi.XFactorStatus) + state.ship.Get(tyAndSashaApi.ExtremeMeasuresStatus);
		return amount;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AVariableHint { status = GadgetManager.GadgetStatus.Status },
				new SmartShieldAction { TargetPlayer = true, Amount = GetXAmount(s), xHint = 1 },
				new AStatus { targetPlayer = true, status = GadgetManager.GadgetStatus.Status, mode = AStatusMode.Set, statusAmount = 0 },
			],
			a: () => [
				new AStatus { targetPlayer = true, status = Status.maxShield, statusAmount = 1 },
				new AVariableHint { status = GadgetManager.GadgetStatus.Status },
				new SmartShieldAction { TargetPlayer = true, Amount = GetXAmount(s), xHint = 1 },
				new AStatus { targetPlayer = true, status = GadgetManager.GadgetStatus.Status, mode = AStatusMode.Set, statusAmount = 0 },
			],
			b: () => [
				new AStatus { targetPlayer = true, status = GadgetManager.GadgetStatus.Status, statusAmount = -1 },
				new SmartShieldAction { TargetPlayer = true, Amount = 1 },
			]
		);
}