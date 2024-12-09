using daisyowl.text;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class WaterfallModelCard : Card, IRegisterable, IHasCustomCardTraits
{
	[JsonProperty]
	private int TimesAnalyzed;

	[JsonProperty]
	private bool AnalyzedAtAll;
	
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/WaterfallModel.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "WaterfallModel", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	private int TimesNeeded
		=> upgrade.Switch(5,  4, 3);

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { AnalyzeManager.ReevaluatedTrait };

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "WaterfallModel", "description", upgrade.ToString(), state == DB.fakeState && (!AnalyzedAtAll || state.route is not Combat) ? "stateless" : "stateful"], new { Times = Math.Max(TimesNeeded - TimesAnalyzed, 1) });
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 0, unplayable = true, description = description },
			a: () => new() { cost = 0, unplayable = true, description = description },
			b: () => new() { cost = 0, unplayable = true, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [new OnAnalyzeAction { Action = new Action { CardId = uuid, EngineLock = 1 } }],
			a: () => [new OnAnalyzeAction { Action = new Action { CardId = uuid, EngineLock = 1 } }],
			b: () => [new OnAnalyzeAction { Action = new Action { CardId = uuid, EngineLock = 2 } }]
		);

	private sealed class Action : CardAction
	{
		public required int CardId;
		public required int EngineLock;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. EngineLock > 0 ? StatusMeta.GetTooltips(Status.lockdown, EngineLock) : [],
				.. new AEndTurn().GetTooltips(s),
				.. StatusMeta.GetTooltips(GadgetManager.GadgetStatus.Status, 1),
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (s.FindCard(CardId) is not WaterfallModelCard card)
			{
				timer = 0;
				return;
			}

			card.AnalyzedAtAll = true;
			card.TimesAnalyzed++;

			if (EngineLock > 0)
				c.QueueImmediate([
					new AStatus { targetPlayer = true, status = Status.lockdown, statusAmount = EngineLock },
					new AEndTurn(),
				]);

			if (card.TimesAnalyzed >= card.TimesNeeded)
			{
				c.QueueImmediate(new AStatus { targetPlayer = true, status = GadgetManager.GadgetStatus.Status, statusAmount = 1 });
				card.TimesAnalyzed = 0;
			}
		}
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not WaterfallModelCard)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}
