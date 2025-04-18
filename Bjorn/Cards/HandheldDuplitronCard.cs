﻿using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class HandheldDuplitronCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/HandheldDuplitron.png"), StableSpr.cards_BackupStick).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "HandheldDuplitron", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "HandheldDuplitron", "description", upgrade.ToString()]);
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 2, description = description },
			a: () => new() { cost = 2, description = description },
			b: () => new() { cost = 2, exhaust = true, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction { Action = new DuplicateAction() },
			],
			a: () => [
				new ACardSelect { browseSource = CardBrowse.Source.Hand, browseAction = new DuplicateAction() }.SetFilterAnalyzable(true),
				new TooltipAction { Tooltips = new DuplicateAction().GetTooltips(s) },
			],
			b: () => [
				new AnalyzeCostAction { Action = new DuplicateAction { Amount = 4 } },
				new AHurt { targetPlayer = true, hurtAmount = 1 },
			]
		);

	// TODO: pre-check if there are enough cards
	internal sealed class DuplicateAction : CardAction
	{
		public int Amount = 1;

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("cardtrait.temporary")];

		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["action", "Analyze", "uiTitle"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (selectedCard is null)
				return;

			var copy = selectedCard.CopyWithNewId();
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, copy, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, true, permanent: true);

			c.QueueImmediate(new AAddCard
			{
				card = copy,
				destination = CardDestination.Hand,
				amount = Amount,
			});
		}
	}
}