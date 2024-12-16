using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class PrototypeCard : Card, IRegisterable
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
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Prototype.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "name"]).Localize,
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(CardUpgrade.FinallyReallyUpgrade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardUpgrade_FinallyReallyUpgrade_Prefix))
		);
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 0, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "Prototype", "description"]) },
			a: () => new() { cost = 0, exhaust = true },
			b: () => new() { cost = 0, exhaust = true }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new OnAnalyzeAction { Action = new UpgradeAction { CardId = uuid } },
				new OnAnalyzeAction { Action = new ExhaustCardAction { CardId = uuid } },
			],
			a: () => [
				new SmartShieldAction { Amount = 2 },
				new OnAnalyzeAction { Action = new ADrawCard { count = 1 } },
			],
			b: () => [
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 1 },
				new OnAnalyzeAction { Action = new ADrawCard { count = 1 } },
			]
		);

	private static bool CardUpgrade_FinallyReallyUpgrade_Prefix(CardUpgrade __instance, G g, Card newCard)
	{
		if (__instance is not InPlaceCardUpgrade)
			return true;

		var card = g.state.FindCard(newCard.uuid);
		if (card is null)
			return true;

		card.upgrade = newCard.upgrade;
		return false;
	}

	private sealed class UpgradeAction : CardAction
	{
		public required int CardId;

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			if (s.FindCard(CardId) is not { } card || card.upgrade != Upgrade.None)
			{
				timer = 0;
				return baseResult;
			}

			return new InPlaceCardUpgrade { cardCopy = Mutil.DeepCopy(card) };
		}
	}

	private sealed class InPlaceCardUpgrade : CardUpgrade;
}