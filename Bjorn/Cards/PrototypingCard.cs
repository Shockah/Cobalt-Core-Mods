using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class PrototypingCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Prototyping.png"), StableSpr.cards_Terminal).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototyping", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 2);

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Prototyping", "description", flipped ? "flipped" : "normal"]);
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 0, exhaust = true, floppable = true, description = description },
			a: () => new() { cost = 0, infinite = true, floppable = true, description = description },
			b: () => new() { cost = 1, floppable = true, description = description }
		);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade.Switch<IReadOnlySet<ICardTraitEntry>>(
			none: () => ImmutableHashSet<ICardTraitEntry>.Empty,
			a: () => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait },
			b: () => ImmutableHashSet<ICardTraitEntry>.Empty
		);

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var card = new PrototypeCard();
		return [
			new AAddCard { destination = CardDestination.Hand, card = card, disabled = flipped },
			new TinkerAction { CardId = card.uuid, disabled = flipped },
			new TinkerAnyAction { disabled = !flipped },
		];
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not PrototypingCard)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}