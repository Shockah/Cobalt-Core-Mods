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
		return upgrade switch
		{
			Upgrade.A => new() { cost = 0, exhaust = true, description = description },
			Upgrade.B => new() { cost = 2, exhaust = true, description = description },
			_ => new() { cost = 0, singleUse = true, description = description },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [new Action { Temporary = true }],
			Upgrade.B => [new Action()],
			_ => [new Action()],
		};

	private sealed class Action : CardAction
	{
		public bool Temporary;
		
		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTCard { card = new BreadnaughtBasicMinigunCard { temporaryOverride = Temporary } },
				new TTCard { card = new BreadnaughtBasicPushCard { temporaryOverride = Temporary } },
				new TTCard { card = new BreadnaughtBasicWeakenCard { temporaryOverride = Temporary } },
			];

		public override Route BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;
			return ModEntry.Instance.KokoroApi.CardDestination.ModifyCardReward(
				new CardReward
				{
					cards = [
						new BreadnaughtBasicMinigunCard { drawAnim = 1, flipAnim = 1 },
						new BreadnaughtBasicPushCard { drawAnim = 1, flipAnim = 1 },
						new BreadnaughtBasicWeakenCard { drawAnim = 1, flipAnim = 1 },
					],
					canSkip = false,
				}
			).SetDestination(CardDestination.Hand).AsRoute;
		}
	}
}