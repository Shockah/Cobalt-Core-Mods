using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ITempHullApi TempHull { get; } = new TempHullApi();
		
		public sealed class TempHullApi : IKokoroApi.IV2.ITempHullApi
		{
			public IKokoroApi.IV2.ITempHullApi.ILoseAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.ITempHullApi.ILoseAction;

			public IKokoroApi.IV2.ITempHullApi.ILoseAction MakeLoseAction(int amount, bool targetPlayer = true)
				=> new TempHullManager.LoseAction { TargetPlayer = targetPlayer, Amount = amount };
		}
	}
}

internal sealed class TempHullManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static readonly TempHullManager Instance = new();

	private TempHullManager()
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	internal void RegainHullNow(State state, Combat combat, int amount, bool targetPlayer)
	{
		if (amount <= 0)
			return;

		var target = targetPlayer ? state.ship : combat.otherShip;
		target.Heal(amount);
		ModEntry.Instance.Helper.ModData.RemoveModData(target, "TempHullLostThisTurn");
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		if (__instance.otherShip.hull > 0)
			return;
		Instance.RegainHullNow(g.state, __instance, g.state.ship.Get(ModEntry.Instance.Content.RegainHullLaterStatus.Status), true);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not LoseAction loseAction)
			return true;
		if (loseAction.TargetPlayer)
			return true;

		loseAction.TargetPlayer = true;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += 10;

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, loseAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();

		loseAction.TargetPlayer = false;

		return false;
	}
	
	public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
		=> args.NonZeroStatuses.Contains(ModEntry.Instance.Content.RegainHullLaterStatus.Status) ? new HashSet<Status> { ModEntry.Instance.Content.RegainHullLaterStatus.Status } : [];

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;
		
		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(args.Ship, "TempHullLostThisTurn"))
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(args.Ship, "TempHullLostThisTurn");
			return false;
		}

		args.Amount = 0;
		return false;
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return;
		if (args.NewAmount != 0)
			return;
		if (args.OldAmount == args.NewAmount)
			return;
		
		RegainHullNow(args.State, args.Combat, args.OldAmount, args.Ship.isPlayerShip);
	}

	internal sealed class LoseAction : CardAction, IKokoroApi.IV2.ITempHullApi.ILoseAction
	{
		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public bool TargetPlayer { get; set; } = true;
		public int Amount { get; set; }
		public bool CannotKill { get; set; }

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. TargetPlayer ? Array.Empty<Tooltip>() : [new GlossaryTooltip($"action.{GetType().Namespace!}::Outgoing")
				{
					Icon = (Spr)ModEntry.Instance.Content.TempHullLossSprite.Id!.Value,
					TitleColor = Colors.keyword,
					Title = ModEntry.Instance.Localizations.Localize(["tempHullLoss", "outgoing", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["tempHullLoss", "outgoing", "description"]),
				}],
				new GlossaryTooltip($"action.{GetType().Namespace!}::TempHullLoss")
				{
					Icon = (Spr)ModEntry.Instance.Content.TempHullLossSprite.Id!.Value,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["tempHullLoss", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["tempHullLoss", "description"]),
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var target = TargetPlayer ? s.ship : c.otherShip;
			var realAmount = CannotKill ? Math.Min(target.hull - 1, Amount) : Amount;
			if (realAmount <= 0)
				return;
			
			ModEntry.Instance.Helper.ModData.SetModData(target, "TempHullLostThisTurn", true);
			c.QueueImmediate([
				new AHurt { targetPlayer = TargetPlayer, hurtAmount = realAmount, cannotKillYou = CannotKill, timer = 0 },
				new AStatus { targetPlayer = TargetPlayer, status = ModEntry.Instance.Content.RegainHullLaterStatus.Status, statusAmount = realAmount },
			]);
		}

		public IKokoroApi.IV2.ITempHullApi.ILoseAction SetTargetPlayer(bool value)
		{
			this.TargetPlayer = value;
			return this;
		}

		public IKokoroApi.IV2.ITempHullApi.ILoseAction SetAmount(int value)
		{
			this.Amount = value;
			return this;
		}

		public IKokoroApi.IV2.ITempHullApi.ILoseAction SetCannotKill(bool value)
		{
			this.CannotKill = value;
			return this;
		}
	}
}
