using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IOnDiscardApi OnDiscard { get; } = new OnDiscardApi();
		
		public sealed class OnDiscardApi : IKokoroApi.IV2.IOnDiscardApi
		{
			public IKokoroApi.IV2.IOnDiscardApi.IOnDiscardAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IOnDiscardApi.IOnDiscardAction;

			public IKokoroApi.IV2.IOnDiscardApi.IOnDiscardAction MakeAction(CardAction action)
				=> new OnDiscardManager.TriggerAction { Action = action };
		}
	}
}

internal sealed class OnDiscardManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly OnDiscardManager Instance = new();
	
	private static ISpriteEntry ActionIcon = null!;

	private static Card? LastCardPlayed;

	internal static void Setup(IHarmony harmony)
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/OnDiscard.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToDiscard_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	public IEnumerable<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is TriggerAction triggerAction ? [triggerAction.Action] : null;

	private static void Combat_TryPlayCard_Prefix(Card card)
		=> LastCardPlayed = card;

	private static void Combat_TryPlayCard_Finalizer()
		=> LastCardPlayed = null;

	private static void Combat_SendCardToDiscard_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.isPlayerTurn)
			return;
		if (card == LastCardPlayed)
			return;

		var meta = card.GetMeta();
		if (s.CharacterIsMissing(meta.deck))
			return;

		__instance.QueueImmediate(
			card.GetActionsOverridden(s, __instance)
				.Where(action => !action.disabled)
				.OfType<TriggerAction>()
				.Select(triggerAction => triggerAction.Action)
				.Select(action =>
				{
					action.whoDidThis = meta.deck;
					return action;
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

	internal sealed class TriggerAction : CardAction, IKokoroApi.IV2.IOnDiscardApi.IOnDiscardAction
	{
		public required CardAction Action { get; set; }

		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{GetType().Namespace!}::OnDiscard")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["onDiscard", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["onDiscard", "description"]),
				},
				.. Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
		
		public IKokoroApi.IV2.IOnDiscardApi.IOnDiscardAction SetAction(CardAction value)
		{
			this.Action = value;
			return this;
		}
	}
}