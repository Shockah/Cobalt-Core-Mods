using HarmonyLib;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public CardAction MakeContinue(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Continue = true };
		}

		public CardAction MakeContinued(Guid id, CardAction action)
			=> new AContinued { Id = id, Continue = true, Action = action };

		public IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeContinued(id, a));

		public CardAction MakeStop(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Continue = false };
		}

		public CardAction MakeStopped(Guid id, CardAction action)
			=> new AContinued { Id = id, Continue = false, Action = action };

		public IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeStopped(id, a));
	}
}

internal sealed class ContinueStopActionManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}
	
	private static void Combat_DrainCardActions_Prefix(Combat __instance, out bool __state)
		=> __state = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;

	private static void Combat_DrainCardActions_Postfix(Combat __instance, ref bool __state)
	{
		var isWorkingOnActions = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;
		if (isWorkingOnActions || !__state)
			return;

		ModEntry.Instance.Api.ObtainExtensionData(__instance, "ContinueFlags", () => new HashSet<Guid>()).Clear();
		ModEntry.Instance.Api.ObtainExtensionData(__instance, "StopFlags", () => new HashSet<Guid>()).Clear();
	}
	
	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AContinued continuedAction)
			return true;
		if (continuedAction.Action is not { } wrappedAction)
			return false;

		var oldActionDisabled = wrappedAction.disabled;
		wrappedAction.disabled = action.disabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;
		if (wrappedAction is AAttack attack)
		{
			var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
			if (shouldStun)
				attack.stunEnemy = shouldStun;
		}

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		wrappedAction.disabled = oldActionDisabled;

		return false;
	}
}

public sealed class AContinue : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public Guid Id;
	public bool Continue;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var continueFlags = Instance.Api.ObtainExtensionData(c, Continue ? "ContinueFlags" : "StopFlags", () => new HashSet<Guid>());
		continueFlags.Add(Id);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"AContinue.{(Continue ? "Continue" : "Stop")}")
			{
				Icon = (Spr)(Continue ? ModEntry.Instance.Content.ContinueSprite : ModEntry.Instance.Content.StopSprite).Id!.Value,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize([Continue ? "continue" : "stop", "name"]),
				Description = ModEntry.Instance.Localizations.Localize([Continue ? "continue" : "stop", "description"]),
			}
		];

	public override Icon? GetIcon(State s)
		=> new(
			path: (Spr)(Continue ? ModEntry.Instance.Content.ContinueSprite : ModEntry.Instance.Content.StopSprite).Id!.Value,
			number: null,
			color: Colors.white
		);
}

public sealed class AContinued : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public Guid Id;
	public bool Continue;
	public CardAction? Action;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Action is null)
			return;

		var continueFlags = Instance.Api.ObtainExtensionData(c, Continue ? "ContinueFlags" : "StopFlags", () => new HashSet<Guid>());
		var hasFlag = continueFlags.Contains(Id);

		if (Continue == !hasFlag)
			return;
		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> Action?.omitFromTooltips == true ? [] : (Action?.GetTooltips(s) ?? []);
}