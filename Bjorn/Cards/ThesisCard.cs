using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class ThesisCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Thesis.png"), StableSpr.cards_BackupStick).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Thesis", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Thesis", "description", upgrade.ToString()]);
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 1, description = description },
			a: () => new() { cost = 1, description = description },
			b: () => new() { cost = 0, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new DrawAction { Extra = 1 },
			],
			a: () => [
				new DrawAction { Extra = 2 },
			],
			b: () => [
				new DrawAction { Extra = 1, Random = true },
			]
		);

	// TODO: pre-check if there are enough cards
	internal sealed class DrawAction : CardAction
	{
		public int Extra;
		public bool Random;

		public override List<Tooltip> GetTooltips(State s)
			=> AnalyzeManager.GetAnalyzeTooltips(s);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (Random)
			{
				var analyzableCards = c.hand.Where(card => card.IsAnalyzable(s, c)).ToList();
				if (analyzableCards.Count == 0)
				{
					timer = 0;
					return;
				}

				var card = analyzableCards[s.rngActions.NextInt() % analyzableCards.Count];
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, true, permanent: false);
				c.QueueImmediate(new ADrawCard { count = card.GetCurrentCost(s) + Extra });
			}
			else
			{
				c.QueueImmediate(new ACardSelect
				{
					browseAction = new BrowseAction { Extra = Extra },
					browseSource = CardBrowse.Source.Hand,
				}.SetFilterAnalyzable(true));
			}
		}

		private sealed class BrowseAction : CardAction
		{
			public required int Extra;

			public override string GetCardSelectText(State s)
				=> ModEntry.Instance.Localizations.Localize(["action", "Analyze", "uiTitle"]);

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				timer = 0;

				if (selectedCard is null)
					return;
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, AnalyzeManager.AnalyzedTrait, true, permanent: false);
				c.QueueImmediate(new ADrawCard { count = selectedCard.GetCurrentCost(s) + Extra });
			}
		}
	}
}