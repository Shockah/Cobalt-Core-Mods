using System;
using System.Collections.Generic;
using System.Reflection;
using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class HarmonizeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Harmonize.png"), StableSpr.cards_TrashFumes).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Harmonize", "name"]).Localize
		});
		
		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new CardRenderingHook());
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Harmonize", "description", upgrade.ToString()]);
		return new() { cost = 2, description = description };
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				new EqualizeAction { timer = 0.2 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				new HarmonizeAction { Amount = 1, timer = 0.1 },
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 1, timer = 0.1 },
				new AStatus { targetPlayer = true, status = AuraManager.FeedbackStatus.Status, statusAmount = 1, timer = 0.1 },
				new AStatus { targetPlayer = true, status = AuraManager.InsightStatus.Status, statusAmount = 1, timer = 0.1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				new HarmonizeAction { Amount = 2, timer = 0.2 },
			],
		};

	private sealed class HarmonizeAction : CardAction
	{
		public required int Amount;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. StatusMeta.GetTooltips(AuraManager.VeilingStatus.Status, Amount),
				.. StatusMeta.GetTooltips(AuraManager.FeedbackStatus.Status, Amount),
				.. StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, Amount),
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var veiling = s.ship.Get(AuraManager.VeilingStatus.Status);
			var feedback = s.ship.Get(AuraManager.FeedbackStatus.Status);
			var insight = s.ship.Get(AuraManager.InsightStatus.Status);
			
			if (veiling == feedback && veiling == insight)
			{
				timer = 0;
				return;
			}
			var min = Math.Min(Math.Min(veiling, feedback), insight);
			
			List<CardAction> actions = [];
			if (veiling == min)
				actions.Add(new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = Amount, timer = timer });
			if (feedback == min)
				actions.Add(new AStatus { targetPlayer = true, status = AuraManager.FeedbackStatus.Status, statusAmount = Amount, timer = timer });
			if (insight == min)
				actions.Add(new AStatus { targetPlayer = true, status = AuraManager.InsightStatus.Status, statusAmount = Amount, timer = timer });
			c.QueueImmediate(actions);
		}
	}
	
	private sealed class EqualizeAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
		{
			var veiling = s.ship.Get(AuraManager.VeilingStatus.Status);
			var feedback = s.ship.Get(AuraManager.FeedbackStatus.Status);
			var insight = s.ship.Get(AuraManager.InsightStatus.Status);
			var max = Math.Max(Math.Max(Math.Max(veiling, feedback), insight), 1);
			return
			[
				.. StatusMeta.GetTooltips(AuraManager.VeilingStatus.Status, max),
				.. StatusMeta.GetTooltips(AuraManager.FeedbackStatus.Status, max),
				.. StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, max),
			];
		}
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var veiling = s.ship.Get(AuraManager.VeilingStatus.Status);
			var feedback = s.ship.Get(AuraManager.FeedbackStatus.Status);
			var insight = s.ship.Get(AuraManager.InsightStatus.Status);

			if (veiling == feedback && veiling == insight)
			{
				timer = 0;
				return;
			}
			var max = Math.Max(Math.Max(veiling, feedback), insight);

			List<CardAction> actions = [];
			if (veiling < max)
				actions.Add(new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = AuraManager.VeilingStatus.Status, statusAmount = max - veiling, timer = timer });
			if (feedback < max)
				actions.Add(new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = AuraManager.FeedbackStatus.Status, statusAmount = max - feedback, timer = timer });
			if (insight < max)
				actions.Add(new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = AuraManager.InsightStatus.Status, statusAmount = max - insight, timer = timer });
			c.QueueImmediate(actions);
		}
	}

	private sealed class CardRenderingHook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
			=> args.Card is HarmonizeCard && args.Card.upgrade != Upgrade.None ? ModEntry.Instance.KokoroApi.Assets.PinchCompactFont : null;
	}
}
