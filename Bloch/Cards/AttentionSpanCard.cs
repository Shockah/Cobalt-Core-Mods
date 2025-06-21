using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class AttentionSpanCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/AttentionSpan.png"), StableSpr.cards_Ace).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AttentionSpan", "name"]).Localize
		});
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait };

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			recycle = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 2
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 2
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.IntensifyStatus.Status,
					statusAmount = 1
				}).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 2
				}).AsCardAction,
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new AStatus
				{
					targetPlayer = true,
					status = AuraManager.IntensifyStatus.Status,
					statusAmount = 1
				}).AsCardAction,
			]
		};
}
