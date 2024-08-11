using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class SafetyProtocolCard : Card, IRegisterable
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
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SafetyProtocol.png"), StableSpr.cards_Shield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SafetyProtocol", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 0, floppable = true, infinite = true },
			a: () => new() { cost = 0, floppable = true, infinite = true },
			b: () => new() { cost = 0, floppable = true }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction { Action = new SmartShieldAction { Amount = 1 }, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					ModEntry.Instance.KokoroApi.ActionCosts.Cost(ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource(), 1),
					new SmartShieldAction { Amount = 1 }
				).Disabled(!flipped),
			],
			a: () => [
				new AnalyzeCostAction { Action = ModEntry.Instance.KokoroApi.Actions.MakeContinue(out var analyzeContinueId), disabled = flipped },
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					ModEntry.Instance.KokoroApi.ActionCosts.Cost(ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource(), 1),
					ModEntry.Instance.KokoroApi.Actions.MakeContinue(out var energyContinueId)
				).Disabled(!flipped),
				new ADummyAction(),
				.. ModEntry.Instance.KokoroApi.Actions.MakeContinued(
					flipped ? energyContinueId : analyzeContinueId,
					[
						new SmartShieldAction { Amount = 1 },
						new ADrawCard { count = 1 }
					]
				),
			],
			b: () => [
				new AnalyzeCostAction { Action = new SmartShieldAction { Amount = 2 }, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					ModEntry.Instance.KokoroApi.ActionCosts.Cost(ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource(), 1),
					new SmartShieldAction { Amount = 2 }
				).Disabled(!flipped),
			]
		);
}