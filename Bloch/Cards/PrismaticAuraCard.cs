using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class PrismaticAuraCard : Card, IRegisterable
{
	private const int OnPlayID = 1000;
	private const int OnDiscardID = 2000;
	private const int OnTurnEndID = 3000;

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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PrismaticAura.png"), StableSpr.cards_Prism).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PrismaticAura", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "ffffff",
			cost = upgrade == Upgrade.B ? 1 : 0,
			description = upgrade == Upgrade.B ? ModEntry.Instance.Localizations.Localize(["card", "PrismaticAura", "description", upgrade.ToString()]) : null,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.InsightStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1
				},
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.InsightStatus.Status,
					statusAmount = 1
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 1
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1
				}).AsCardAction,
			],
			_ => [
				ModEntry.Instance.Api.MakeChooseAura(
					card: this,
					amount: upgrade == Upgrade.A ? 2 : 1,
					uiSubtitle: ModEntry.Instance.Api.GetChooseAuraOnPlayUISubtitle(upgrade == Upgrade.A ? 2 : 1),
					actionId: OnPlayID
				),
				ModEntry.Instance.KokoroApi.OnTurnEnd.MakeAction(ModEntry.Instance.Api.MakeChooseAura(
					card: this,
					amount: upgrade == Upgrade.A ? 2 : 1,
					uiSubtitle: ModEntry.Instance.Api.GetChooseAuraOnTurnEndUISubtitle(upgrade == Upgrade.A ? 2 : 1),
					actionId: OnTurnEndID
				)).AsCardAction,
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(ModEntry.Instance.Api.MakeChooseAura(
					card: this,
					amount: upgrade == Upgrade.A ? 2 : 1,
					uiSubtitle: ModEntry.Instance.Api.GetChooseAuraOnDiscardUISubtitle(upgrade == Upgrade.A ? 2 : 1),
					actionId: OnDiscardID
				)).AsCardAction,
			]
		};
}
