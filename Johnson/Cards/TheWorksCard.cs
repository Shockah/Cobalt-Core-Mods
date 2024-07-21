using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class TheWorksCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/TheWorks.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "TheWorks", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 0,
			exhaust = upgrade != Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "TheWorks", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAddCard { card = new LayoutCard { temporaryOverride = true }, destination = CardDestination.Hand },
				new AAddCard { card = new StrategizeCard { temporaryOverride = true }, destination = CardDestination.Hand },
			],
			Upgrade.B => [
				new ASpecificCardOffering
				{
					Destination = CardDestination.Deck,
					Cards = [
						new LayoutCard { temporaryOverride = true },
						new StrategizeCard { temporaryOverride = true },
					],
				},
				new ATooltipAction
				{
					Tooltips = [
						new TTCard { card = new LayoutCard { temporaryOverride = true } },
						new TTCard { card = new StrategizeCard { temporaryOverride = true } },
					]
				},
			],
			_ => [
				new ASpecificCardOffering
				{
					Destination = CardDestination.Hand,
					Cards = [
						new LayoutCard { temporaryOverride = true },
						new StrategizeCard { temporaryOverride = true },
					],
				},
				new ATooltipAction
				{
					Tooltips = [
						new TTCard { card = new LayoutCard { temporaryOverride = true } },
						new TTCard { card = new StrategizeCard { temporaryOverride = true } },
					]
				},
			]
		};
}
