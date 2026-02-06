using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class ApproachShiftCard : Card, IRegisterable, IHasCustomCardTraits
{
	private static Spr LeftSprite;
	private static Spr RightSprite;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		LeftSprite = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ApproachShiftLeft.png"), StableSpr.cards_ScootLeft).Sprite;
		RightSprite = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ApproachShiftRight.png"), StableSpr.cards_ScootRight).Sprite;
		
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = RightSprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ApproachShift", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
	{
		var data = new CardData { cost = 0, flippable = true, art = flipped ? LeftSprite : RightSprite };
		return upgrade switch
		{
			Upgrade.B => data,
			Upgrade.A => data with { exhaust = true, retain = true },
			_ => data with { exhaust = true },
		};
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait },
			_ => new HashSet<ICardTraitEntry>(),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 }
			).AsCardAction,
			new AMove { targetPlayer = true, dir = 1 },
		];
}
