using daisyowl.text;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class ChangePerspectiveCard : Card, IRegisterable
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
			Art = StableSpr.cards_CloudSave,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/ChangePerspective.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ChangePerspective", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			infinite = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "ChangePerspective", "description"]),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new Action()];

	private sealed class Action : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new CardRoute();
	}

	private sealed class CardRoute : Route
	{
		private const UK ChoiceKey = (UK)2137521;

		public List<Status> ConvertFrom = [
			AuraManager.VeilingStatus.Status,
			AuraManager.FeedbackStatus.Status,
			AuraManager.InsightStatus.Status,
		];

		public List<Status> ConvertTo = [
			AuraManager.VeilingStatus.Status,
			AuraManager.FeedbackStatus.Status,
			AuraManager.InsightStatus.Status,
		];

		public int ConvertFromAmount = 1;
		public int ConvertToAmount = 2;

		private int? ConvertFromSelected;
		private int? ConvertToSelected;

		public override bool GetShowOverworldPanels()
			=> true;

		public override bool CanBePeeked()
			=> true;

		public override void Render(G g)
		{
			base.Render(g);
			if (!ConvertFrom.Any(status => g.state.ship.Get(status) >= ConvertFromAmount))
			{
				g.CloseRoute(this);
				return;
			}

			int centerX = 240;
			int topY = 80;

			int choiceWidth = 56;
			int choiceHeight = 18;
			int choiceSpacing = 4;
			int actionSpacing = 4;
			int actionYOffset = 7;
			int actionHoverYOffset = 1;

			SharedArt.DrawEngineering(g);

			Draw.Text(ModEntry.Instance.Localizations.Localize((["card", "ChangePerspective", "uiTitle"])), centerX, topY, font: DB.stapler, color: Colors.textMain, align: TAlign.Center);

			void RenderChoices(List<Status> choices, int amount, Func<int?> isSelected, Func<int, bool> inactiveGetter, Action<int> choiceSetter, int y)
			{
				var rowWidth = choices.Count * choiceWidth + Math.Max(choices.Count - 1, 0) * choiceSpacing;
				var rowStartX = centerX - rowWidth / 2;
				for (var i = 0; i < choices.Count; i++)
				{
					var ii = i;
					var choice = choices[i];
					var inactive = inactiveGetter(i);
					var selected = isSelected() == i;
					var fakeAction = new AStatus { targetPlayer = true, status = choice, statusAmount = amount, disabled = inactive };
					var choiceStartX = rowStartX + (choiceWidth + choiceSpacing) * i;
					var choiceTopY = topY + 24;

					var buttonRect = new Rect(choiceStartX, choiceTopY + y, choiceWidth, choiceHeight);
					var buttonResult = SharedArt.ButtonText(
						g, Vec.Zero, new UIKey(ChoiceKey, y * 10 + i), "", rect: buttonRect,
						inactive: inactive,
						onMouseDown: new MouseDownHandler(() => choiceSetter(ii)),
						boxColor: inactive ? Colors.buttonInactive : Colors.buttonBoxNormal,
						showAsPressed: selected
					);

					var isHover = g.boxes.FirstOrDefault(b => b.key == new UIKey(ChoiceKey, y * 10 + i))?.IsHover() == true;
					if (isHover)
						g.tooltips.Add(new Vec(buttonRect.x + buttonRect.w, buttonRect.y + buttonRect.h), StatusMeta.GetTooltips(choice, ConvertFromAmount));

					var actionWidth = RenderAction(g, g.state, fakeAction, dontDraw: true);
					var actionStartX = choiceStartX + 3 + choiceWidth / 2 - actionWidth / 2;
					var actionXOffset = 0;

					g.Push(rect: new(actionStartX + actionXOffset, choiceTopY + y + actionYOffset + (selected || (isHover && !inactive) ? actionHoverYOffset : 0)));
					actionXOffset += RenderAction(g, g.state, fakeAction, dontDraw: false) + actionSpacing;
					g.Pop();
				}
			}

			RenderChoices(ConvertFrom, ConvertFromAmount, () => ConvertFromSelected, i => g.state.ship.Get(ConvertFrom[i]) < ConvertFromAmount, i => ConvertFromSelected = i, 0);
			RenderChoices(ConvertTo, ConvertToAmount, () => ConvertToSelected, _ => false, i => ConvertToSelected = i, 60);

			{
				var rect = new Rect(centerX - 14, topY + 55, 33, 24);
				RotatedButtonSprite(g, rect, StableUK.btn_move_right, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: false, noHover: true);
			}

			var inactive = ConvertFromSelected is not { } convertFrom || ConvertToSelected is not { } convertTo || g.state.ship.Get(ConvertFrom[convertFrom]) < ConvertFromAmount;
			SharedArt.ButtonText(
				g,
				new Vec(210, 205),
				(UIKey)(UK)21375001,
				ModEntry.Instance.Localizations.Localize(["route", "MultiCardBrowse", "doneButton"]),
				boxColor: inactive ? Colors.buttonInactive : null,
				inactive: inactive,
				onMouseDown: new MouseDownHandler(() => OnFinishChoosing(g))
			);
		}

		private void OnFinishChoosing(G g)
		{
			if (ConvertFromSelected is not { } convertFrom || ConvertToSelected is not { } convertTo || g.state.ship.Get(ConvertFrom[convertFrom]) < ConvertFromAmount)
			{
				Audio.Play(Event.ZeroEnergy);
				return;
			}

			(g.state.route as Combat)?.QueueImmediate([
				new AStatus
				{
					targetPlayer = true,
					status = ConvertFrom[convertFrom],
					statusAmount = -ConvertFromAmount
				},
				new AStatus
				{
					targetPlayer = true,
					status = ConvertTo[convertTo],
					statusAmount = ConvertToAmount
				}
			]);
			g.CloseRoute(this);
		}

		// mostly copy-paste of SharedArt.ButtonResult, without too many improvements
		private static SharedArt.ButtonResult RotatedButtonSprite(G g, Rect rect, UIKey key, Spr sprite, Spr spriteHover, Spr? spriteDown = null, Color? boxColor = null, bool inactive = false, bool flipX = false, bool flipY = false, OnMouseDown? onMouseDown = null, bool autoFocus = false, bool noHover = false, bool showAsPressed = false, bool gamepadUntargetable = false, UIKey? leftHint = null, UIKey? rightHint = null)
		{
			var box = g.Push(key, rect, null, autoFocus, inactive, gamepadUntargetable, ReticleMode.Quad, onMouseDown, null, null, null, 0, rightHint, leftHint);
			var xy = box.rect.xy;
			var isPressed = !noHover && (box.IsHover() || showAsPressed) && !inactive;
			if (spriteDown.HasValue && box.IsHover() && Input.mouseLeft)
				showAsPressed = true;
			var rotation = Math.PI / 2;
			Draw.Sprite((!showAsPressed) ? (isPressed ? spriteHover : sprite) : (spriteDown ?? spriteHover), xy.x + Math.Sin(rotation) * rect.w, xy.y - Math.Cos(rotation) * rect.h, flipX, flipY, rotation, null, null, null, null, boxColor);
			SharedArt.ButtonResult buttonResult = default;
			buttonResult.isHover = isPressed;
			buttonResult.FIXME_isHoverForTooltip = !noHover && box.IsHover();
			buttonResult.v = xy;
			buttonResult.innerOffset = new Vec(0.0, showAsPressed ? 2 : (isPressed ? 1 : 0));
			g.Pop();
			return buttonResult;
		}
	}
}
