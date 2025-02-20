using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class DrawNowPrototypeTinkerCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		PrototypeTinkerManager.RegisterEntry("DrawNow", new TinkerConfiguration
		{
			TinkerType = typeof(AttackPrototypeTinker),
			CardFactory = _ => new DrawNowPrototypeTinkerCard(),
		}, -100);
		
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "tinker", "DrawNow", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			description = ModEntry.Instance.Localizations.Localize(["card", "Prototype", "tinker", "DrawNow", "description"]),
			singleUse = true,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new ADrawCard { count = 1 }];
}

[UsedImplicitly]
public sealed class DrawNowPrototypeTinker : ITinker
{
	public int GetTinkerCost(State state, Card card, int level)
		=> 0;
	
	public string GetCardNameSuffix(State state, Card card, int level)
		=> "";
}