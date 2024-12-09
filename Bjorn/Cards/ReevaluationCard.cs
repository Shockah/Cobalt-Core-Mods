using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class ReevaluationCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Reevaluation.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Reevaluation", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Reevaluation", "description", upgrade.ToString()]);
		return upgrade.Switch<CardData>(
			() => new() { cost = 0, exhaust = true, description = description },
			() => new() { cost = 0, description = description },
			() => new() { cost = 0, exhaust = true, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new MainAction { RemoveAnalyzed = true, AddReevaluated = true },
				}.SetFilterReevaluated(false),
				new TooltipAction { Tooltips = [
					.. AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [],
					.. AnalyzeManager.ReevaluatedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [],
				] },
			],
			a: () => [
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new MainAction { RemoveAnalyzed = true, AddReevaluated = true },
				}.SetFilterReevaluated(false),
				new TooltipAction { Tooltips = [
					.. AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [],
					.. AnalyzeManager.ReevaluatedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [],
				] },
			],
			b: () => [
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new MainAction { RemoveAnalyzed = true, Permanent = true },
				}.SetFilterReevaluated(false),
				new TooltipAction { Tooltips = [
					.. AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [],
				] },
				new DowngradeAction { CardId = uuid },
			]
		);

	private sealed class MainAction : CardAction
	{
		public bool RemoveAnalyzed;
		public bool AddReevaluated;
		public bool Permanent;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. RemoveAnalyzed ? AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [] : [],
				.. AddReevaluated ? AnalyzeManager.ReevaluatedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [] : [],
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
			{
				timer = 0;
				return;
			}
			
			if (RemoveAnalyzed)
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, AnalyzeManager.AnalyzedTrait, false, permanent: Permanent);
			if (AddReevaluated)
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, AnalyzeManager.ReevaluatedTrait, true, permanent: Permanent);
		}
	}

	private sealed class DowngradeAction : CardAction
	{
		public required int CardId;
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (s.FindCard(CardId) is not { } card)
			{
				timer = 0;
				return;
			}
			
			if (ModEntry.Instance.KokoroApi.TemporaryUpgrades.GetTemporaryUpgrade(card) is { } temporaryUpgrade && temporaryUpgrade != Upgrade.None)
				ModEntry.Instance.KokoroApi.TemporaryUpgrades.SetTemporaryUpgrade(card, Upgrade.None);
			else if (ModEntry.Instance.KokoroApi.TemporaryUpgrades.GetPermanentUpgrade(card) != Upgrade.None)
				ModEntry.Instance.KokoroApi.TemporaryUpgrades.SetPermanentUpgrade(card, Upgrade.None);
		}
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not ReevaluationCard || args.Card.upgrade == Upgrade.B)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}
