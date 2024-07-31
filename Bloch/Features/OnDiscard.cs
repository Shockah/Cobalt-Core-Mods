using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class OnDiscardManager : IWrappedActionHook
{
	private static ISpriteEntry ActionIcon = null!;

	private static Card? LastCardPlayed = null;

	public OnDiscardManager()
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/OnDiscard.png"));

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Finalizer))
		);
		ModEntry.Instance.Helper.Utilities.Harmony.Patch( // gets inlined when delayed
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_SendCardToDiscard_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);

		ModEntry.Instance.KokoroApi.Actions.RegisterWrappedActionHook(this, 0);
	}

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
		if (s.CharacterIsMissing(card.GetMeta().deck))
			return;

		var meta = card.GetMeta();
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

		bool oldActionDisabled = triggerAction.Action.disabled;
		triggerAction.Action.disabled = triggerAction.disabled;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

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

	public List<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is TriggerAction triggerAction ? [triggerAction.Action] : null;

	internal sealed class TriggerAction : CardAction
	{
		public required CardAction Action;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{GetType().Namespace!}::OnDiscard")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "OnDiscard", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "OnDiscard", "description"]),
				},
				..Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is MuscleMemoryArtifact) is not { } artifact)
				return;

			if (string.IsNullOrEmpty(Action.artifactPulse))
				Action.artifactPulse = artifact.Key();
			c.QueueImmediate(Action);
		}
	}
}