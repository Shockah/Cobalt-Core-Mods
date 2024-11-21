using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public CardAction MakeContinue(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Type = IKokoroApi.IV2.IContinueStopApi.ActionType.Continue };
		}

		public CardAction MakeContinued(Guid id, CardAction action)
			=> new AContinued { Ids = [id], Type = IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, Operator = IKokoroApi.IV2.IContinueStopApi.FlagOperator.And, Action = action };

		public IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeContinued(id, a));

		public CardAction MakeStop(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Type = IKokoroApi.IV2.IContinueStopApi.ActionType.Stop };
		}

		public CardAction MakeStopped(Guid id, CardAction action)
			=> new AContinued { Ids = [id], Type = IKokoroApi.IV2.IContinueStopApi.ActionType.Stop, Operator = IKokoroApi.IV2.IContinueStopApi.FlagOperator.Or, Action = action };

		public IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeStopped(id, a));
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IContinueStopApi ContinueStop { get; } = new ContinueStopApi();
		
		public sealed class ContinueStopApi : IKokoroApi.IV2.IContinueStopApi
		{
			public IKokoroApi.IV2.IContinueStopApi.ITriggerAction? AsTriggerAction(CardAction action)
				=> action as IKokoroApi.IV2.IContinueStopApi.ITriggerAction;

			public IKokoroApi.IV2.IContinueStopApi.ITriggerAction MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType type, out Guid id)
			{
				id = Guid.NewGuid();
				return new AContinue { Id = id, Type = type };
			}

			public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction? AsFlaggedAction(CardAction action)
				=> action as IKokoroApi.IV2.IContinueStopApi.IFlaggedAction;

			public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction MakeFlaggedAction(IKokoroApi.IV2.IContinueStopApi.ActionType type, Guid id, CardAction action)
				=> MakeFlaggedAction(type, new HashSet<Guid> { id }, action);

			public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction MakeFlaggedAction(IKokoroApi.IV2.IContinueStopApi.ActionType type, IEnumerable<Guid> ids, CardAction action)
				=> new AContinued { Ids = ids.ToHashSet(), Type = type, Operator = type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? IKokoroApi.IV2.IContinueStopApi.FlagOperator.And : IKokoroApi.IV2.IContinueStopApi.FlagOperator.Or, Action = action };

			public IEnumerable<IKokoroApi.IV2.IContinueStopApi.IFlaggedAction> MakeFlaggedActions(IKokoroApi.IV2.IContinueStopApi.ActionType type, Guid id, IEnumerable<CardAction> actions)
				=> actions.Select(action => MakeFlaggedAction(type, id, action));

			public IEnumerable<IKokoroApi.IV2.IContinueStopApi.IFlaggedAction> MakeFlaggedActions(IKokoroApi.IV2.IContinueStopApi.ActionType type, IEnumerable<Guid> ids, IEnumerable<CardAction> actions)
				=> actions.Select(action => MakeFlaggedAction(type, ids, action));
		}
	}
}

internal sealed class ContinueStopActionManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly ContinueStopActionManager Instance = new();
	
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
	
	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
	{
		if (args.Action is not AContinued continued)
			return null;
		if (continued.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
	
	private static void Combat_DrainCardActions_Prefix(Combat __instance, out bool __state)
		=> __state = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;

	private static void Combat_DrainCardActions_Postfix(Combat __instance, ref bool __state)
	{
		var isWorkingOnActions = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;
		if (isWorkingOnActions || !__state)
			return;

		ModEntry.Instance.Helper.ModData.ObtainModData(__instance, "ContinueFlags", () => new HashSet<Guid>()).Clear();
		ModEntry.Instance.Helper.ModData.ObtainModData(__instance, "StopFlags", () => new HashSet<Guid>()).Clear();
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

public sealed class AContinue : CardAction, IKokoroApi.IV2.IContinueStopApi.ITriggerAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public required IKokoroApi.IV2.IContinueStopApi.ActionType Type { get; set; }
	public required Guid Id { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var continueFlags = Instance.Helper.ModData.ObtainModData(c, Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? "ContinueFlags" : "StopFlags", () => new HashSet<Guid>());
		continueFlags.Add(Id);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"AContinue.{(Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? "Continue" : "Stop")}")
			{
				Icon = (Spr)(Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? ModEntry.Instance.Content.ContinueSprite : ModEntry.Instance.Content.StopSprite).Id!.Value,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize([Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? "continue" : "stop", "name"]),
				Description = ModEntry.Instance.Localizations.Localize([Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? "continue" : "stop", "description"]),
			}
		];

	public override Icon? GetIcon(State s)
		=> new(
			path: (Spr)(Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? ModEntry.Instance.Content.ContinueSprite : ModEntry.Instance.Content.StopSprite).Id!.Value,
			number: null,
			color: Colors.white
		);
	
	public IKokoroApi.IV2.IContinueStopApi.ITriggerAction SetType(IKokoroApi.IV2.IContinueStopApi.ActionType value)
	{
		Type = value;
		return this;
	}

	public IKokoroApi.IV2.IContinueStopApi.ITriggerAction SetId(Guid value)
	{
		Id = value;
		return this;
	}
}

public sealed class AContinued : CardAction, IKokoroApi.IV2.IContinueStopApi.IFlaggedAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public required IKokoroApi.IV2.IContinueStopApi.ActionType Type { get; set; }
	public required HashSet<Guid> Ids { get; set; }
	public required IKokoroApi.IV2.IContinueStopApi.FlagOperator Operator { get; set; }
	public required CardAction Action { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var continueFlags = Instance.Helper.ModData.ObtainModData(c, Type == IKokoroApi.IV2.IContinueStopApi.ActionType.Continue ? "ContinueFlags" : "StopFlags", () => new HashSet<Guid>());
		if (!ShouldContinue())
			return;

		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);

		bool ShouldContinue()
		{
			switch (Type)
			{
				case IKokoroApi.IV2.IContinueStopApi.ActionType.Continue:
					switch (Operator)
					{
						case IKokoroApi.IV2.IContinueStopApi.FlagOperator.And:
							if (Ids.Any(id => !continueFlags.Contains(id)))
								return false;
							return true;
						case IKokoroApi.IV2.IContinueStopApi.FlagOperator.Or:
							if (Ids.Any(id => continueFlags.Contains(id)))
								return true;
							return false;
						default:
							throw new ArgumentOutOfRangeException();
					}
				case IKokoroApi.IV2.IContinueStopApi.ActionType.Stop:
					switch (Operator)
					{
						case IKokoroApi.IV2.IContinueStopApi.FlagOperator.And:
							if (Ids.Any(id => !continueFlags.Contains(id)))
								return true;
							return false;
						case IKokoroApi.IV2.IContinueStopApi.FlagOperator.Or:
							if (Ids.Any(id => continueFlags.Contains(id)))
								return false;
							return true;
						default:
							throw new ArgumentOutOfRangeException();
					}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public override List<Tooltip> GetTooltips(State s)
		=> Action?.omitFromTooltips == true ? [] : (Action?.GetTooltips(s) ?? []);
	
	public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction SetType(IKokoroApi.IV2.IContinueStopApi.ActionType value)
	{
		Type = value;
		return this;
	}

	public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction SetIds(HashSet<Guid> value)
	{
		Ids = value;
		return this;
	}

	public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction SetOperator(IKokoroApi.IV2.IContinueStopApi.FlagOperator value)
	{
		Operator = value;
		return this;
	}

	public IKokoroApi.IV2.IContinueStopApi.IFlaggedAction SetAction(CardAction value)
	{
		Action = value;
		return this;
	}
}