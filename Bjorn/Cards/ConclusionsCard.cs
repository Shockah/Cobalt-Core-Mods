using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using daisyowl.text;
using Shockah.Kokoro;

namespace Shockah.Bjorn;

public sealed class ConclusionsCard : Card, IRegisterable
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
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Conclusions.png"), StableSpr.cards_BackupStick).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Conclusions", "name"]).Localize,
		});

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	public override CardData GetData(State state)
	{
		return upgrade.Switch<CardData>(
			() => new()
			{
				cost = 2, exhaust = true,
				description = ModEntry.Instance.Localizations.Localize(
					["card", "Conclusions", "description", upgrade.ToString(), state.route is Combat ? "stateful" : "stateless"], 
					new { Amount = (state.route as Combat)?.hand.Count(card => card != this && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, AnalyzeManager.AnalyzedTrait)) ?? 0 }
				),
			},
			() => new()
			{
				cost = 2, exhaust = true, retain = true,
				description = ModEntry.Instance.Localizations.Localize(
					["card", "Conclusions", "description", upgrade.ToString(), state.route is Combat ? "stateful" : "stateless"], 
					new { Amount = (state.route as Combat)?.hand.Count(card => card != this && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, AnalyzeManager.AnalyzedTrait)) ?? 0 }
				),
			},
			() => new()
			{
				cost = 1,
				description = ModEntry.Instance.Localizations.Localize(
					["card", "Conclusions", "description", upgrade.ToString(), state.route is Combat ? "stateful" : "stateless"], 
					new { Damage = (ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1) * 2 }
				),
			}
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = GadgetManager.GadgetStatus.Status, statusAmount = 2 },
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat).AsCardAction,
				new AHurt { targetPlayer = true, hurtShieldsFirst = true, hurtAmount = (ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1) * 2, xHint = 2, omitFromTooltips = true },
			],
			_ => [
				new AnalyzedInHandVariableHint { IgnoreCardId = uuid },
				new AStatus { targetPlayer = true, status = GadgetManager.GadgetStatus.Status, statusAmount = c.hand.Count(card => card != this && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, AnalyzeManager.AnalyzedTrait)), xHint = 1 },
			]
		};

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not ConclusionsCard || args.Card.upgrade != Upgrade.B)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}