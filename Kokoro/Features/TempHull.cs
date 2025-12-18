using System;
using System.Collections.Generic;
using System.Linq;
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
			public Status LoseHullLaterStatus
				=> ModEntry.Instance.Content.LoseHullLaterStatus.Status;
			
			public Status RegainHullLaterStatus
				=> ModEntry.Instance.Content.RegainHullLaterStatus.Status;

			public IKokoroApi.IV2.ITempHullApi.IGainAction? AsGainAction(CardAction action)
				=> action as IKokoroApi.IV2.ITempHullApi.IGainAction;

			public IKokoroApi.IV2.ITempHullApi.IGainAction MakeGainAction(int amount, bool targetPlayer = true)
				=> new TempHullManager.GainAction { TargetPlayer = targetPlayer, Amount = amount };

			public IKokoroApi.IV2.ITempHullApi.ILossAction? AsLossAction(CardAction action)
				=> action as IKokoroApi.IV2.ITempHullApi.ILossAction;

			public IKokoroApi.IV2.ITempHullApi.ILossAction MakeLossAction(int amount, bool targetPlayer = true)
				=> new TempHullManager.LossAction { TargetPlayer = targetPlayer, Amount = amount };
		}
	}
}

internal sealed class TempHullManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static readonly TempHullManager Instance = new();
	
	private static readonly Lazy<HashSet<Status>> StatusesToCallTurnTriggerHooksFor = new(() => [
		ModEntry.Instance.Content.LoseHullLaterStatus.Status,
		ModEntry.Instance.Content.RegainHullLaterStatus.Status,
	]);

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
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Heal)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_Heal_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_Heal_Postfix))
		);
	}

	private static void RegainHullNow(State state, Combat combat, int amount, bool targetPlayer)
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
		
		RegainHullNow(g.state, __instance, g.state.ship.Get(ModEntry.Instance.Content.RegainHullLaterStatus.Status), true);
		g.state.ship.Set(ModEntry.Instance.Content.RegainHullLaterStatus.Status, 0);

		var hullToLose = g.state.ship.Get(ModEntry.Instance.Content.LoseHullLaterStatus.Status);
		var realAmount = Math.Min(g.state.ship.hull - 1, hullToLose);
		if (realAmount <= 0)
			return;

		g.state.ship.Set(ModEntry.Instance.Content.LoseHullLaterStatus.Status, 0);
		g.state.ship.DirectHullDamage(g.state, __instance, realAmount);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not LossAction loseAction)
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

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, out int __state)
		=> __state = __instance.hull;

	private static void Ship_DirectHullDamage_Postfix(Ship __instance, in int __state)
	{
		var lostAmount = __state - __instance.hull;
		if (lostAmount <= 0)
			return;

		var tempHull = __instance.Get(ModEntry.Instance.Content.LoseHullLaterStatus.Status);
		var statusToLose = Math.Min(lostAmount, tempHull);
		if (statusToLose < 0)
			return;

		__instance.Add(ModEntry.Instance.Content.LoseHullLaterStatus.Status, -statusToLose);
	}

	private static void Ship_Heal_Prefix(Ship __instance, out int __state)
		=> __state = __instance.hull;

	private static void Ship_Heal_Postfix(Ship __instance, int amount, in int __state)
	{
		var healedAmount = __instance.hull - __state;
		if (healedAmount == amount)
			return;

		var remainingHeal = amount - healedAmount;
		var tempHull = __instance.Get(ModEntry.Instance.Content.LoseHullLaterStatus.Status);
		var statusToLose = Math.Min(remainingHeal, tempHull);
		if (statusToLose < 0)
			return;

		__instance.Add(ModEntry.Instance.Content.LoseHullLaterStatus.Status, -statusToLose);
	}
	
	public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
		=> StatusesToCallTurnTriggerHooksFor.Value.Intersect(args.NonZeroStatuses).ToHashSet();

	public bool? IsAffectedByBoost(IKokoroApi.IV2.IStatusLogicApi.IHook.IIsAffectedByBoostArgs args)
		=> StatusesToCallTurnTriggerHooksFor.Value.Contains(args.Status) ? false : null;

	public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
	{
		if (!StatusesToCallTurnTriggerHooksFor.Value.Contains(args.Status))
			return args.NewAmount;
		if (args.NewAmount <= args.OldAmount)
			return args.NewAmount;

		var opposite = args.Status == ModEntry.Instance.Content.LoseHullLaterStatus.Status ? ModEntry.Instance.Content.RegainHullLaterStatus.Status : ModEntry.Instance.Content.LoseHullLaterStatus.Status;
		var oppositeAmount = args.Ship.Get(opposite);

		var toRemove = Math.Min(args.NewAmount - args.OldAmount, oppositeAmount);
		args.Ship.Add(opposite, -toRemove);
		return args.NewAmount - toRemove;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status == ModEntry.Instance.Content.RegainHullLaterStatus.Status)
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
		
		if (args.Status == ModEntry.Instance.Content.LoseHullLaterStatus.Status)
		{
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return false;
			
			args.Amount = 0;
			return false;
		}
		
		return false;
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status == ModEntry.Instance.Content.RegainHullLaterStatus.Status)
		{
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
				return;
			if (args.NewAmount != 0)
				return;
			if (args.OldAmount == args.NewAmount)
				return;
		
			RegainHullNow(args.State, args.Combat, args.OldAmount, args.Ship.isPlayerShip);
			return;
		}

		if (args.Status == ModEntry.Instance.Content.LoseHullLaterStatus.Status)
		{
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return;
			if (args.NewAmount != 0)
				return;
			if (args.OldAmount == args.NewAmount)
				return;
			
			args.Ship.DirectHullDamage(args.State, args.Combat, args.OldAmount - args.NewAmount);
			return;
		}
	}
	
	internal sealed class GainAction : CardAction, IKokoroApi.IV2.ITempHullApi.IGainAction
	{
		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public bool TargetPlayer { get; set; } = true;
		public int Amount { get; set; }

		public override Icon? GetIcon(State s)
			=> new((Spr)ModEntry.Instance.Content.TempHullGainSprite.Id!.Value, Amount, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. TargetPlayer ? Array.Empty<Tooltip>() : [new GlossaryTooltip($"action.{GetType().Namespace!}::Outgoing")
				{
					Icon = StableSpr.icons_outgoing,
					TitleColor = Colors.keyword,
					Title = ModEntry.Instance.Localizations.Localize(["tempHullGain", "outgoing", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["tempHullGain", "outgoing", "description"]),
				}],
				new GlossaryTooltip($"action.{GetType().Namespace!}::TempHullGain")
				{
					Icon = (Spr)ModEntry.Instance.Content.TempHullGainSprite.Id!.Value,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["tempHullGain", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["tempHullGain", "description"]),
					vals = [Amount],
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var target = TargetPlayer ? s.ship : c.otherShip;
			var realAmount = Math.Min(target.hullMax - target.hull, Amount);
			if (realAmount <= 0)
				return;
			
			target.Heal(realAmount);
			c.QueueImmediate(new AStatus { targetPlayer = TargetPlayer, status = ModEntry.Instance.Content.LoseHullLaterStatus.Status, statusAmount = realAmount });
		}

		public IKokoroApi.IV2.ITempHullApi.IGainAction SetTargetPlayer(bool value)
		{
			this.TargetPlayer = value;
			return this;
		}

		public IKokoroApi.IV2.ITempHullApi.IGainAction SetAmount(int value)
		{
			this.Amount = value;
			return this;
		}
	}

	internal sealed class LossAction : CardAction, IKokoroApi.IV2.ITempHullApi.ILossAction
	{
		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public bool TargetPlayer { get; set; } = true;
		public int Amount { get; set; }
		public bool CannotKill { get; set; }

		public override Icon? GetIcon(State s)
			=> new((Spr)ModEntry.Instance.Content.TempHullLossSprite.Id!.Value, Amount, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. TargetPlayer ? Array.Empty<Tooltip>() : [new GlossaryTooltip($"action.{GetType().Namespace!}::Outgoing")
				{
					Icon = StableSpr.icons_outgoing,
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
					vals = [Amount],
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

		public IKokoroApi.IV2.ITempHullApi.ILossAction SetTargetPlayer(bool value)
		{
			this.TargetPlayer = value;
			return this;
		}

		public IKokoroApi.IV2.ITempHullApi.ILossAction SetAmount(int value)
		{
			this.Amount = value;
			return this;
		}

		public IKokoroApi.IV2.ITempHullApi.ILossAction SetCannotKill(bool value)
		{
			this.CannotKill = value;
			return this;
		}
	}
}
