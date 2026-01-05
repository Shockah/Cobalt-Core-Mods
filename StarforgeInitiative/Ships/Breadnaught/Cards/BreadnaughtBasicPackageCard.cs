using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtBasicPackageCard : CannonColorless, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Card/BasicPackage.png"), StableSpr.cards_ColorlessTrash).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "card", "BasicPackage", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["ship", "Breadnaught", "card", "BasicPackage", "description", upgrade.ToString()]);
		return new() { cost = 0, exhaust = true, description = description };
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new Action { Upgrade = upgrade }];

	private sealed class Action : CardAction
	{
		public Upgrade Upgrade = Upgrade.None;
		
		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTCard { card = new BreadnaughtBasicMinigunCard { upgrade = Upgrade } },
				new TTCard { card = new BreadnaughtBasicPushCard { upgrade = Upgrade } },
				new TTCard { card = new BreadnaughtBasicOverdriveCard { upgrade = Upgrade } },
				new TTCard { card = new BreadnaughtBasicWeakenCard { upgrade = Upgrade } },
			];

		public override Route BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;
			return ModEntry.Instance.KokoroApi.CardDestination.ModifyCardReward(
				new CardReward
				{
					cards = [
						new BreadnaughtBasicMinigunCard { drawAnim = 1, flipAnim = 1, upgrade = Upgrade },
						new BreadnaughtBasicPushCard { drawAnim = 1, flipAnim = 1, upgrade = Upgrade },
						new BreadnaughtBasicOverdriveCard { drawAnim = 1, flipAnim = 1, upgrade = Upgrade },
						new BreadnaughtBasicWeakenCard { drawAnim = 1, flipAnim = 1, upgrade = Upgrade },
					],
					canSkip = false,
				}
			).SetDestination(CardDestination.Hand).AsRoute;
		}
	}
}