using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class RelativityPrototypeTinkerCard : Card, IRegisterable
{
	private static TinkerEntry TinkerEntry = null!;

	public required int CardId;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TinkerEntry = PrototypeTinkerManager.RegisterEntry("Relativity", new TinkerConfiguration
		{
			TinkerType = typeof(RelativityPrototypeTinker),
			CardFactory = prototype => new RelativityPrototypeTinkerCard { CardId = prototype.uuid },
		});
		
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				dontOffer = true,
				unreleased = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PrototypeTinker.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "tinker", "Relativity", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		state = MG.inst.g?.state ?? state;
		if (state.FindCard(CardId) is not PrototypeCard card)
			return default;

		var level = card.Tinkers.FirstOrDefault(t => t.Tinker is RelativityPrototypeTinker)?.Level ?? 0;
		return new()
		{
			cost = PrototypeTinkerManager.GetTinkerUpgradeCost(state, card),
			description = ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "Relativity", "description", level == 0 ? "apply" : "upgrade"], new { Gain = level }),
			singleUse = true,
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new LevelUpTinkerAction { TinkerUniqueName = TinkerEntry.UniqueName, CardId = CardId },
			new TooltipAction { Tooltips = StatusMeta.GetTooltips(RelativityManager.RelativityStatus.Status, 1) },
		];
}

[UsedImplicitly]
public sealed class RelativityPrototypeTinker : ITinker
{
	public string GetCardNameSuffix(State state, Card card, int level)
		=> string.Concat(Enumerable.Repeat(ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "Relativity", "cardNameSuffix"]), level));

	public IEnumerable<CardAction> GetActions(State state, Combat combat, Card card, int level)
	{
		if (level % 2 == 0)
			return [new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = level / 2 }];
		
		if (level == 1)
			return [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					card.uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2,
					new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 1 }
				).AsCardAction
			];
		
		return [
			ModEntry.Instance.KokoroApi.Sequence.MakeAction(
				card.uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2,
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = level / 2 + 1 }
			).AsCardAction,
			ModEntry.Instance.KokoroApi.Sequence.MakeAction(
				card.uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2,
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = level / 2 }
			).AsCardAction
		];
	}
}