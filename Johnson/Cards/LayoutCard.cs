using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class LayoutCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Layout.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Layout", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Layout", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAddCard
				{
					card = new BulletPointCard(),
					destination = CardDestination.Discard,
				},
				new AAddCard
				{
					card = new SlideTransitionCard(),
					destination = CardDestination.Discard,
				}
			],
			_ => [
				new ASpecificCardOffering
				{
					Destination = CardDestination.Deck,
					Cards = [
						new BulletPointCard(),
						new SlideTransitionCard(),
					]
				},
				new ATooltipAction
				{
					Tooltips = [
						new TTCard { card = new BulletPointCard() },
						new TTCard { card = new SlideTransitionCard() },
					]
				}
			]
		};
}
