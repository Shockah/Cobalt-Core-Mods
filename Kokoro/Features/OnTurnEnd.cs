using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IOnTurnEndApi OnTurnEnd { get; } = new OnTurnEndApi();
		
		public sealed class OnTurnEndApi : IKokoroApi.IV2.IOnTurnEndApi
		{
			public IKokoroApi.IV2.IOnTurnEndApi.IOnTurnEndAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IOnTurnEndApi.IOnTurnEndAction;

			public IKokoroApi.IV2.IOnTurnEndApi.IOnTurnEndAction MakeAction(CardAction action)
				=> new OnTurnEndManager.TriggerAction { Action = action };
		}
	}
}

internal sealed class OnTurnEndManager : IWrappedActionHook
{
	internal static readonly OnTurnEndManager Instance = new();
	
	private static ISpriteEntry ActionIcon = null!;

	internal static void Setup(IHarmony harmony)
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/OnTurnEnd.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	internal static void SetupLate()
		=> WrappedActionManager.Instance.Register(Instance, 0);
	
	public List<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is TriggerAction triggerAction ? [triggerAction.Action] : null;

	private static void AEndTurn_Begin_Prefix(State s, Combat c)
	{
		if (c.cardActions.Any(a => a is AEndTurn))
			return;

		c.QueueImmediate(
			c.hand
				.Where(card => !s.CharacterIsMissing(card.GetMeta().deck))
				.SelectMany(card =>
				{
					var meta = card.GetMeta();
					return card.GetActionsOverridden(s, c)
						.Where(action => !action.disabled)
						.OfType<TriggerAction>()
						.Select(triggerAction => triggerAction.Action)
						.Select(action =>
						{
							action.whoDidThis = meta.deck;
							return action;
						});
				})
		);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not TriggerAction triggerAction)
			return true;

		var oldActionDisabled = triggerAction.Action.disabled;
		triggerAction.Action.disabled = triggerAction.disabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ActionIcon.Sprite, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += 10;

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, triggerAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		triggerAction.Action.disabled = oldActionDisabled;

		return false;
	}

	internal sealed class TriggerAction : CardAction, IKokoroApi.IV2.IOnTurnEndApi.IOnTurnEndAction
	{
		public required CardAction Action { get; set; }

		public CardAction AsCardAction
			=> this;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{GetType().Namespace!}::OnTurnEnd")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["onTurnEnd", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["onTurnEnd", "description"]),
				},
				.. Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
	}
}