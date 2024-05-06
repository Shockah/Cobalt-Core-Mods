using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class PrismaticAuraCard : Card, IRegisterable
{
	private static ISpriteEntry ChooseAuraIcon = null!;

	public Status? PlayStatus;
	public Status? OnDiscardStatus;
	public Status? OnTurnEndStatus;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ChooseAuraIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Prismatic.png"));

		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PrismaticAura.png"), StableSpr.cards_Prism).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PrismaticAura", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "ffffff",
			cost = upgrade == Upgrade.B ? 1 : 0,
			description = upgrade == Upgrade.B ? ModEntry.Instance.Localizations.Localize(["card", "PrismaticAura", "description", upgrade.ToString()]) : null,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.InsightStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.InsightStatus.Status,
						statusAmount = 1
					}
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.FeedbackStatus.Status,
						statusAmount = 1
					}
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.VeilingStatus.Status,
						statusAmount = 1
					}
				},
			],
			_ => [
				PlayStatus is { } playStatus
					? new AStatus
					{
						targetPlayer = true,
						status = playStatus,
						statusAmount = upgrade == Upgrade.A ? 2 : 1
					}
					: new ChoiceAction
					{
						Amount = upgrade == Upgrade.A ? 2 : 1,
						CardId = uuid,
						PlayMode = PlayMode.Play
					},
				new OnTurnEndManager.TriggerAction
				{
					Action = OnTurnEndStatus is { } onTurnStatus
						? new AStatus
						{
							targetPlayer = true,
							status = onTurnStatus,
							statusAmount = upgrade == Upgrade.A ? 2 : 1
						}
						: new ChoiceAction
						{
							Amount = upgrade == Upgrade.A ? 2 : 1,
							CardId = uuid,
							PlayMode = PlayMode.OnTurnEnd
						}
				},
				new OnDiscardManager.TriggerAction
				{
					Action = OnDiscardStatus is { } onDiscardStatus
						? new AStatus
						{
							targetPlayer = true,
							status = onDiscardStatus,
							statusAmount = upgrade == Upgrade.A ? 2 : 1
						}
						: new ChoiceAction
						{
							Amount = upgrade == Upgrade.A ? 2 : 1,
							CardId = uuid,
							PlayMode = PlayMode.OnDiscard
						}
				},
			]
		};

	public override void OnExitCombat(State s, Combat c)
	{
		base.OnExitCombat(s, c);
		PlayStatus = null;
		OnDiscardStatus = null;
		OnTurnEndStatus = null;
	}

	public enum PlayMode
	{
		Play,
		OnDiscard,
		OnTurnEnd
	}

	private sealed class ChoiceAction : CardAction
	{
		public required int Amount;
		public required int CardId;
		public required PlayMode PlayMode;

		public override Icon? GetIcon(State s)
			=> new(ChooseAuraIcon.Sprite, Amount, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::PrismaticAura::Choose")
				{
					Icon = ChooseAuraIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["card", "PrismaticAura", "action", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["card", "PrismaticAura", "action", "description"], new { Amount }),
				},
				..StatusMeta.GetTooltips(AuraManager.VeilingStatus.Status, Math.Max(s.ship.Get(AuraManager.VeilingStatus.Status), Amount)),
				..StatusMeta.GetTooltips(AuraManager.FeedbackStatus.Status, Math.Max(s.ship.Get(AuraManager.FeedbackStatus.Status), Amount)),
				..StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, Math.Max(s.ship.Get(AuraManager.InsightStatus.Status), Amount)),
			];

		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new ChoiceRoute { Amount = Amount, CardId = CardId, PlayMode = PlayMode };
	}

	private sealed class ChoiceRoute : Route
	{
		private const UK ChoiceKey = (UK)2137522;

		public required int Amount;
		public required int CardId;
		public required PlayMode PlayMode;

		public List<Status> Statuses = [
			AuraManager.VeilingStatus.Status,
			AuraManager.FeedbackStatus.Status,
			AuraManager.InsightStatus.Status
		];

		public override bool GetShowOverworldPanels()
			=> true;

		public override bool CanBePeeked()
			=> true;

		public override void Render(G g)
		{
			base.Render(g);

			int centerX = 240;
			int topY = 80;

			int choiceWidth = 56;
			int choiceHeight = 24;
			int choiceSpacing = 4;
			int actionSpacing = 4;
			int actionYOffset = 7;
			int actionHoverYOffset = 1;

			SharedArt.DrawEngineering(g);

			Draw.Text(ModEntry.Instance.Localizations.Localize((["card", "PrismaticAura", "uiTitle"])), centerX, topY, font: DB.stapler, color: Colors.textMain, align: TAlign.Center);
			Draw.Text(ModEntry.Instance.Localizations.Localize((["card", "PrismaticAura", "uiSubtitle", PlayMode.ToString()])), centerX, topY + 24, color: Colors.textMain, align: TAlign.Center);

			var rowWidth = Statuses.Count * choiceWidth + Math.Max(Statuses.Count - 1, 0) * choiceSpacing;
			var rowStartX = centerX - rowWidth / 2;
			for (var i = 0; i < Statuses.Count; i++)
			{
				var ii = i;
				var choice = Statuses[i];
				var fakeAction = new AStatus { targetPlayer = true, status = choice, statusAmount = Amount };
				var choiceStartX = rowStartX + (choiceWidth + choiceSpacing) * i;
				var choiceTopY = topY + 48;

				var buttonRect = new Rect(choiceStartX, choiceTopY, choiceWidth, choiceHeight);
				var buttonResult = SharedArt.ButtonText(
					g, Vec.Zero, new UIKey(ChoiceKey, i), "", rect: buttonRect,
					onMouseDown: new MouseDownHandler(() => OnFinishChoosing(g, ii))
				);

				var isHover = g.boxes.FirstOrDefault(b => b.key == new UIKey(ChoiceKey, i))?.IsHover() == true;
				if (isHover)
					g.tooltips.Add(new Vec(buttonRect.x + buttonRect.w, buttonRect.y + buttonRect.h), StatusMeta.GetTooltips(choice, Amount));

				var actionWidth = RenderAction(g, g.state, fakeAction, dontDraw: true);
				var actionStartX = choiceStartX + 3 + choiceWidth / 2 - actionWidth / 2;
				var actionXOffset = 0;

				g.Push(rect: new(actionStartX + actionXOffset, choiceTopY + actionYOffset + (isHover ? actionHoverYOffset : 0)));
				actionXOffset += RenderAction(g, g.state, fakeAction, dontDraw: false) + actionSpacing;
				g.Pop();
			}
		}

		private void OnFinishChoosing(G g, int choiceIndex)
		{
			if (g.state.FindCard(CardId) is not PrismaticAuraCard card)
			{
				g.CloseRoute(this);
				return;
			}

			var choice = Statuses[choiceIndex];
			(g.state.route as Combat)?.QueueImmediate(new AStatus
			{
				targetPlayer = true,
				status = choice,
				statusAmount = Amount
			});

			switch (PlayMode)
			{
				case PlayMode.Play:
					card.PlayStatus = choice;
					break;
				case PlayMode.OnDiscard:
					card.OnDiscardStatus = choice;
					break;
				case PlayMode.OnTurnEnd:
					card.OnTurnEndStatus = choice;
					break;
			}

			g.CloseRoute(this);
		}
	}
}
