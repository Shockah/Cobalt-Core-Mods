using System;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;
using daisyowl.text;
using Shockah.Kokoro;

namespace Shockah.Bjorn;

public sealed class AdjustCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Adjust.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Adjust", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 3);

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			() => new() { cost = 0, description = ModEntry.Instance.Localizations.Localize(["card", "Adjust", "description", upgrade.ToString()]) },
			() => new() { cost = 0, retain = true, description = ModEntry.Instance.Localizations.Localize(["card", "Adjust", "description", upgrade.ToString()]) },
			() => new() { cost = 0, floppable = true, description = ModEntry.Instance.Localizations.Localize(["card", "Adjust", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]) }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction { Deanalyze = false, Action = new Action { Discount = -1 } },
			],
			a: () => [
				new AnalyzeCostAction { Deanalyze = false, Action = new Action { Discount = -1 } }
			],
			b: () => [
				new AnalyzeCostAction { Deanalyze = false, Action = new Action { Discount = -1 }, disabled = flipped },
				new AnalyzeCostAction { Deanalyze = true, Action = new Action { Discount = 1 }, disabled = !flipped },
			]
		);

	private sealed class Action : CardAction
	{
		public required int Discount;

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary(Discount < 0 ? "cardtrait.discount" : "cardtrait.expensive", Math.Abs(Discount))];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;

			selectedCard.discount += Discount;
		}
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not AdjustCard || args.Card.upgrade != Upgrade.B)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}
