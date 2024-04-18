using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class OnTurnEndManager
{
	private static ISpriteEntry ActionIcon = null!;

	public OnTurnEndManager()
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/OnTurnEnd.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AEndTurn_Begin_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);
	}

	private static void AEndTurn_Begin_Prefix(State s, Combat c)
	{
		if (c.cardActions.Any(a => a is AEndTurn))
			return;

		c.QueueImmediate(
			c.hand
				.SelectMany(card => card.GetActionsOverridden(s, c))
				.Where(action => !action.disabled)
				.OfType<TriggerAction>()
				.Select(triggerAction => triggerAction.Action)
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

	internal sealed class TriggerAction : CardAction
	{
		public required CardAction Action;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{GetType().Namespace!}::OnTurnEnd")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "OnTurnEnd", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "OnTurnEnd", "description"]),
				},
				..Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
	}
}