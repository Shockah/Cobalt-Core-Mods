using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class DiscountPrototypeTinkerCard : Card, IRegisterable
{
	private static TinkerEntry TinkerEntry = null!;

	public required int CardId;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TinkerEntry = PrototypeTinkerManager.RegisterEntry("Discount", new TinkerConfiguration
		{
			TinkerType = typeof(DiscountPrototypeTinker),
			CardFactory = prototype => new DiscountPrototypeTinkerCard { CardId = prototype.uuid },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "tinker", "Discount", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		state = MG.inst.g?.state ?? state;
		if (state.FindCard(CardId) is not PrototypeCard card)
			return default;

		return new()
		{
			cost = PrototypeTinkerManager.GetTinkerUpgradeCost(state, card) + 1,
			description = ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "Discount", "description"]),
			singleUse = true,
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new LevelUpTinkerAction { TinkerUniqueName = TinkerEntry.UniqueName, CardId = CardId }];
}

[UsedImplicitly]
public sealed class DiscountPrototypeTinker : ITinker
{
	public bool CanUpgradeTo(State state, Card card, int level)
		=> level == 1;
	
	public int GetTinkerCost(State state, Card card, int level)
		=> 2;
	
	public string GetCardNameSuffix(State state, Card card, int level)
		=> string.Concat(Enumerable.Repeat(ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "Discount", "cardNameSuffix"]), level));

	public void ModifyCardData(State state, Card card, int level, ref CardData data)
		=> data.cost = Math.Max(data.cost - 1, 0);
}