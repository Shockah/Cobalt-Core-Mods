using daisyowl.text;
using FSPRO;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class CrystalKnowledgeCard : Card, IRegisterable
{
	private static ISpriteEntry ChooseActionIcon = null!;
	private static ISpriteEntry ChooseActionUpgradedIcon = null!;
	private static ISpriteEntry ButtonSprite = null!;
	private static ISpriteEntry ButtonDownSprite = null!;
	
	[JsonProperty]
	private List<string>? BaseHandlers;
	
	[JsonProperty]
	private List<string>? UpgradedHandlers;

	[JsonIgnore]
	private List<string>? Handlers
		=> upgrade == Upgrade.B ? UpgradedHandlers : BaseHandlers;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ChooseActionIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/CrystalKnowledgeChoose.png"));
		ChooseActionUpgradedIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/CrystalKnowledgeChooseUpgraded.png"));
		ButtonSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/UI/CrystalKnowledgeButton.png"));
		ButtonDownSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/UI/CrystalKnowledgeButtonDown.png"));
		
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/CrystalKnowledge.png"), StableSpr.cards_BooksShard).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CrystalKnowledge", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			() => new() { cost = 1 },
			() => new() { cost = 1 },
			() => new() { cost = 1, exhaust = true }
		);

	private List<CardAction>? GetRawActions(State state, Combat combat)
	{
		if (Handlers is null)
			return null;
		
		var handlers = Handlers.Select(CrystalKnowledgeManager.LookupHandler).ToList();
		if (handlers.Any(h => h is null))
			return null;
		
		var actions = handlers.Select((handler, i) => handler!.MakeAction(state, combat, this, i, upgrade == Upgrade.B)).ToList();
		if (actions.Any(a => a is null))
			return null;

		return actions!;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var rawActions = GetRawActions(s, c);
		var freeActionCount = upgrade switch
		{
			Upgrade.A => 2,
			_ => 0,
		};
		
		List<CardAction> realActions;
		List<CardAction>? renderActions;

		if (rawActions is null)
		{
			realActions = [
				new ChooseActionsAction { CardId = uuid, IsUpgraded = upgrade == Upgrade.B },
				.. Enumerable.Range(0, 4).Select(_ => new ADummyAction()),
			];
			
			renderActions = realActions
				.Select<CardAction, CardAction>((_, i) =>
					i < freeActionCount
						? new ChooseActionsAction { CardId = uuid, IsUpgraded = upgrade == Upgrade.B }
						: new AnalyzeCostAction { CardId = uuid, Count = 1, Action = new ChooseActionsAction { CardId = uuid, IsUpgraded = upgrade == Upgrade.B } }
				)
				.ToList();
		}
		else
		{
			realActions = [
				new AnalyzeCostAction
				{
					Count = 5 - freeActionCount,
					Action = new PlayActionsAction
					{
						CardId = uuid,
						FreeActionCount = freeActionCount,
					}
				},
				.. Enumerable.Range(0, 4).Select(_ => new ADummyAction()),
			];
			
			renderActions = rawActions
				.Select<CardAction, CardAction>((action, i) =>
					i < freeActionCount
						? action
						: new AnalyzeCostAction { CardId = uuid, Count = 1, Action = action }
				)
				.ToList();
		}

		return Enumerable.Range(0, Math.Min(realActions.Count, renderActions.Count))
			.Select(i => ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(renderActions[i], realActions[i]).AsCardAction)
			.ToList();
	}

	private sealed class PlayActionsAction : CardAction
	{
		public required int CardId;
		public int FreeActionCount;
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			if (s.FindCard(CardId) is not CrystalKnowledgeCard card)
				return;
			
			var count = ModEntry.Instance.KokoroApi.MultiCardBrowse.GetSelectedCards(this)?.Count ?? (selectedCard is null ? 0 : 1);
			var actionCount = Math.Min(count + FreeActionCount, 5);
			c.QueueImmediate((card.GetRawActions(s, c) ?? []).Take(actionCount));
		}
	}

	private sealed class ChooseActionsAction : CardAction
	{
		public required int CardId;
		public required bool IsUpgraded;

		public override Icon? GetIcon(State s)
			=> new() { path = (IsUpgraded ? ChooseActionUpgradedIcon : ChooseActionIcon).Sprite };

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(CrystalKnowledgeCard)}::ChooseActions::{(IsUpgraded ? "Upgraded" : "Normal")}")
				{
					Icon = (IsUpgraded ? ChooseActionUpgradedIcon : ChooseActionIcon).Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["card", "CrystalKnowledge", "chooseTooltip", "title", IsUpgraded ? "upgraded" : "normal"]),
					Description = ModEntry.Instance.Localizations.Localize(["card", "CrystalKnowledge", "chooseTooltip", "description", IsUpgraded ? "upgraded" : "normal"]),
				}
			];

		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new ChooseActionsRoute { CardId = CardId, IsUpgraded = IsUpgraded };
	}
	
	internal sealed class ChooseActionsRoute : Route, OnInputPhase
	{
		private static readonly UK ChoiceKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		private static readonly UK ContinueKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

		public required int CardId;
		public required bool IsUpgraded;

		[JsonIgnore]
		private List<(string HandlerName, CardAction?[] Actions)>? Actions;

		[JsonIgnore]
		private string?[] SelectedHandlers = new string?[5];

		public override bool GetShowOverworldPanels()
			=> true;

		public override bool CanBePeeked()
			=> true;

		public override void Render(G g)
		{
			base.Render(g);
			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}
			if (g.state.FindCard(CardId) is not { } card)
			{
				g.CloseRoute(this);
				return;
			}

			Actions ??= CrystalKnowledgeManager.Handlers
				.Where(kvp => kvp.Value.IsEnabled(g.state))
				.OrderBy(kvp => kvp.Key)
				.Select(kvp => (HandlerName: kvp.Key, Actions: Enumerable.Range(0, 5).Select(i => kvp.Value.MakeAction(g.state, combat, card, i, IsUpgraded)).ToArray()))
				.Where(e => e.Actions.Any(a => a is not null))
				.OrderBy(e => e.Actions.FirstIndex(a => a is not null)!.Value)
				.ToList();

			var centerX = 240;
			var topY = 44;

			var columns = 5;
			var choiceWidth = 27;
			var choiceHeight = 25;
			var choiceSpacing = 1;
			var actionYOffset = 7;
			var actionHoverYOffset = 1;

			// ReSharper disable once UselessBinaryOperation
			var fullRowWidth = columns * choiceWidth + Math.Max(columns - 1, 0) * choiceSpacing;
			var choicesStartX = centerX - fullRowWidth / 2;

			SharedArt.DrawEngineering(g);

			Draw.Text("CHOOSE ACTIONS", centerX, topY, font: DB.stapler, color: Colors.textMain, align: TAlign.Center);

			for (var rowIndex = 0; rowIndex < Actions.Count; rowIndex++)
			{
				var row = Actions[rowIndex];
				var rowWidth = columns * choiceWidth + Math.Max(columns - 1, 0) * choiceSpacing;
				var rowStartX = choicesStartX + (fullRowWidth - rowWidth) / 2;

				for (var columnIndex = 0; columnIndex < columns; columnIndex++)
				{
					if (row.Actions[columnIndex] is not { } choice)
						continue;
					
					var columnIndex2 = columnIndex;
					var choiceStartX = rowStartX + (choiceWidth + choiceSpacing) * columnIndex;
					var choiceTopY = topY + 24 + rowIndex * (choiceHeight + choiceSpacing);

					var showAsPressed = SelectedHandlers[columnIndex] == row.HandlerName;
					var buttonRect = new Rect(choiceStartX, choiceTopY, choiceWidth, choiceHeight);
					var buttonResult = SharedArt.ButtonText(
						g, Vec.Zero, new UIKey(ChoiceKey, rowIndex * columns + columnIndex), "", rect: buttonRect,
						onMouseDown: new MouseDownHandler(() => OnChoice(columnIndex2, row.HandlerName)),
						sprite: showAsPressed ? ButtonDownSprite.Sprite : ButtonSprite.Sprite,
						spriteHover: ButtonDownSprite.Sprite,
						spriteDown: ButtonDownSprite.Sprite
					);

					if (buttonResult.isHover)
						g.tooltips.Add(new Vec(buttonRect.x + buttonRect.w, buttonRect.y + buttonRect.h), choice.GetTooltips(g.state));

					var actionWidth = RenderAction(g, g.state, choice, dontDraw: true);
					var actionStartX = choiceStartX + choiceWidth / 2 - actionWidth / 2;
					
					g.Push(rect: new(actionStartX, choiceTopY + actionYOffset + (showAsPressed || buttonResult.isHover ? actionHoverYOffset : 0)));
					RenderAction(g, g.state, choice, dontDraw: false);
					g.Pop();
				}
			}

			SharedArt.ButtonText(
				g,
				new Vec(390, 228),
				ContinueKey,
				Loc.T("uiShared.btnContinue"),
				inactive: !CanContinue(),
				onMouseDown: new MouseDownHandler(() => OnContinue(g)),
				platformButtonHint: Btn.Y
			);
		}

		public void OnInputPhase(G g, Box b)
		{
			if (Input.GetGpDown(Btn.Y))
				OnContinue(g);
		}

		private bool CanContinue()
			=> SelectedHandlers.All(handlerName => handlerName is not null);

		private void OnChoice(int tier, string handler)
		{
			Audio.Play(Event.Click);
			
			for (var i = 0; i < SelectedHandlers.Length; i++)
				if (SelectedHandlers[i] == handler)
					SelectedHandlers[i] = null;
			SelectedHandlers[tier] = handler;
		}

		private void OnContinue(G g)
		{
			if (g.state.route is not Combat combat)
				return;
			
			if (g.state.FindCard(CardId) is not CrystalKnowledgeCard card)
			{
				g.CloseRoute(this);
				return;
			}
			
			if (!CanContinue())
			{
				Audio.Play(Event.ZeroEnergy);
				return;
			}

			if (IsUpgraded)
				card.UpgradedHandlers = SelectedHandlers!.ToList<string>();
			else
				card.BaseHandlers = SelectedHandlers!.ToList<string>();
			
			combat.QueueImmediate(card.GetActionsOverridden(g.state, combat));
			combat.QueueImmediate(new ADelay());
			g.CloseRoute(this);
		}
	}
}
