using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class SmartShieldPrototypeTinkerCard : Card, IRegisterable
{
	private static TinkerEntry TinkerEntry = null!;

	public required int CardId;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TinkerEntry = PrototypeTinkerManager.RegisterEntry("SmartShield", new TinkerConfiguration
		{
			TinkerType = typeof(SmartShieldPrototypeTinker),
			CardFactory = prototype => new SmartShieldPrototypeTinkerCard { CardId = prototype.uuid },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "tinker", "SmartShield", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		state = MG.inst.g?.state ?? state;
		if (state.FindCard(CardId) is not PrototypeCard card)
			return default;

		var level = card.Tinkers.FirstOrDefault(t => t.Tinker is SmartShieldPrototypeTinker)?.Level ?? 0;
		return new()
		{
			cost = PrototypeTinkerManager.GetTinkerUpgradeCost(state, card),
			description = ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "SmartShield", "description", level == 0 ? "apply" : "upgrade"], new { Gain = level }),
			singleUse = true,
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new LevelUpTinkerAction { TinkerUniqueName = TinkerEntry.UniqueName, CardId = CardId },
			new TooltipAction { Tooltips = new SmartShieldAction { TargetPlayer = true, Amount = 1 }.GetTooltips(s) },
		];
}

[UsedImplicitly]
public sealed class SmartShieldPrototypeTinker : ITinker
{
	public string GetCardNameSuffix(State state, Card card, int level)
		=> string.Concat(Enumerable.Repeat(ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "SmartShield", "cardNameSuffix"]), level));

	public IEnumerable<CardAction> GetActions(State state, Combat combat, Card card, int level)
		=> [new SmartShieldAction { TargetPlayer = true, Amount = level }];
}