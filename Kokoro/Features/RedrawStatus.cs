using CobaltCoreModding.Definitions.ExternalItems;
using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	public ExternalStatus RedrawStatus
		=> Instance.Content.RedrawStatus;

	public Status RedrawVanillaStatus
		=> (Status)RedrawStatus.Id!.Value;

	public Tooltip GetRedrawStatusTooltip()
		=> new TTGlossary($"status.{Instance.Content.RedrawStatus.Id!.Value}", 1);

	public void RegisterRedrawStatusHook(IRedrawStatusHook hook, double priority)
		=> RedrawStatusManager.Instance.HookManager.Register(hook, priority);

	public void UnregisterRedrawStatusHook(IRedrawStatusHook hook)
		=> RedrawStatusManager.Instance.HookManager.Unregister(hook);

	public bool IsRedrawPossible(State state, Combat combat, Card card)
		=> RedrawStatusManager.Instance.IsRedrawPossible(state, combat, card);

	public bool DoRedraw(State state, Combat combat, Card card)
		=> RedrawStatusManager.Instance.DoRedraw(state, combat, card);

	public IRedrawStatusHook StandardRedrawStatusPaymentHook
		=> RedrawStatusManager.Instance.HookMapper.MapToV1(Kokoro.StandardRedrawStatusPaymentHook.Instance);

	public IRedrawStatusHook StandardRedrawStatusActionHook
		=> RedrawStatusManager.Instance.HookMapper.MapToV1(Kokoro.StandardRedrawStatusActionHook.Instance);
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IRedrawStatusApi RedrawStatus { get; } = new RedrawStatusApi();
		
		public sealed class RedrawStatusApi : IKokoroApi.IV2.IRedrawStatusApi
		{
			public Status Status
				=> (Status)Instance.Content.RedrawStatus.Id!.Value;

			public IKokoroApi.IV2.IRedrawStatusApi.IHook StandardRedrawStatusPaymentHook
				=> Kokoro.StandardRedrawStatusPaymentHook.Instance;
			
			public IKokoroApi.IV2.IRedrawStatusApi.IHook StandardRedrawStatusActionHook
				=> Kokoro.StandardRedrawStatusActionHook.Instance;
			
			public bool IsRedrawPossible(State state, Combat combat, Card card)
				=> RedrawStatusManager.Instance.IsRedrawPossible(state, combat, card);

			public bool DoRedraw(State state, Combat combat, Card card)
				=> RedrawStatusManager.Instance.DoRedraw(state, combat, card);

			public void RegisterHook(IKokoroApi.IV2.IRedrawStatusApi.IHook hook, double priority = 0)
				=> RedrawStatusManager.Instance.HookManager.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IRedrawStatusApi.IHook hook)
				=> RedrawStatusManager.Instance.HookManager.Unregister(hook);
			
			internal sealed class CanRedrawArgs : IKokoroApi.IV2.IRedrawStatusApi.IHook.ICanRedrawArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
			}
			
			internal sealed class PayForRedrawArgs : IKokoroApi.IV2.IRedrawStatusApi.IHook.IPayForRedrawArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook PossibilityHook { get; internal set; } = null!;
			}
			
			internal sealed class DoRedrawArgs : IKokoroApi.IV2.IRedrawStatusApi.IHook.IDoRedrawArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook PossibilityHook { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook PaymentHook { get; internal set; } = null!;
			}
			
			internal sealed class AfterRedrawArgs : IKokoroApi.IV2.IRedrawStatusApi.IHook.IAfterRedrawArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook PossibilityHook { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook PaymentHook { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook ActionHook { get; internal set; } = null!;
			}
			
			internal sealed class OverrideHookEnablementArgs : IKokoroApi.IV2.IRedrawStatusApi.IHook.IOverrideHookEnablementArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IRedrawStatusApi.IHook.HookType HookType { get; internal set; }
				public IKokoroApi.IV2.IRedrawStatusApi.IHook Hook { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class RedrawStatusManager
{
	internal static readonly RedrawStatusManager Instance = new();
	internal readonly BidirectionalHookMapper<IKokoroApi.IV2.IRedrawStatusApi.IHook, IRedrawStatusHook> HookMapper = new(
		hook => new V1ToV2RedrawStatusHookWrapper(hook),
		hook => new V2ToV1RedrawStatusHookWrapper(hook)
	);
	internal readonly VariedApiVersionHookManager<IKokoroApi.IV2.IRedrawStatusApi.IHook, IRedrawStatusHook> HookManager;
	
	public RedrawStatusManager()
	{
		HookManager = new(ModEntry.Instance.Package.Manifest.UniqueName, HookMapper);
		
		HookManager.Register(StandardRedrawStatusPaymentHook.Instance, 0);
		HookManager.Register(StandardRedrawStatusActionHook.Instance, -1000);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix))
		);
	}

	private bool IsHookEnabled(State state, Combat combat, IKokoroApi.IV2.IRedrawStatusApi.IHook.HookType hookType, IKokoroApi.IV2.IRedrawStatusApi.IHook testedHook)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.OverrideHookEnablementArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.HookType = hookType;
			args.Hook = testedHook;

			foreach (var hook in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (hook.OverrideHookEnablement(args) is { } result)
					return result;
			return true;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool IsRedrawPossible(State state, Combat combat, Card card)
	{
		if (!combat.isPlayerTurn)
			return false;
		if (!combat.hand.Contains(card))
			return false;

		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.CanRedrawArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;

			foreach (var hook in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (IsHookEnabled(state, combat, IKokoroApi.IV2.IRedrawStatusApi.IHook.HookType.Possibility, hook) && hook.CanRedraw(args) is { } result)
					return result;
			return false;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool DoRedraw(State state, Combat combat, Card card)
	{
		if (GetPossibilityHook() is not { } possibilityHook)
			return false;
		if (GetPaymentHook() is not { } paymentHook)
			return false;
		if (GetActionHook() is not { } actionHook)
			return false;
		
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.AfterRedrawArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.PossibilityHook = possibilityHook;
			args.PaymentHook = paymentHook;
			args.ActionHook = actionHook;

			foreach (var hook in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				hook.AfterRedraw(args);
			return true;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}

		IKokoroApi.IV2.IRedrawStatusApi.IHook? GetPossibilityHook()
		{
			var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.CanRedrawArgs>();
			try
			{
				args.State = state;
				args.Combat = combat;
				args.Card = card;

				foreach (var hook in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				{
					if (!IsHookEnabled(state, combat, IKokoroApi.IV2.IRedrawStatusApi.IHook.HookType.Possibility, hook))
						continue;

					switch (hook.CanRedraw(args))
					{
						case false:
							return null;
						case true:
							return hook;
					}
				}
				return null;
			}
			finally
			{
				ModEntry.Instance.ArgsPool.Return(args);
			}
		}

		IKokoroApi.IV2.IRedrawStatusApi.IHook? GetPaymentHook()
		{
			var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.PayForRedrawArgs>();
			try
			{
				args.State = state;
				args.Combat = combat;
				args.Card = card;
				args.PossibilityHook = possibilityHook;

				return HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
					.FirstOrDefault(hook => IsHookEnabled(state, combat, IKokoroApi.IV2.IRedrawStatusApi.IHook.HookType.Payment, hook) && hook.PayForRedraw(args));
			}
			finally
			{
				ModEntry.Instance.ArgsPool.Return(args);
			}
		}

		IKokoroApi.IV2.IRedrawStatusApi.IHook? GetActionHook()
		{
			var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.DoRedrawArgs>();
			try
			{
				args.State = state;
				args.Combat = combat;
				args.Card = card;
				args.PossibilityHook = possibilityHook;
				args.PaymentHook = paymentHook;

				return HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
					.FirstOrDefault(hook => IsHookEnabled(state, combat, IKokoroApi.IV2.IRedrawStatusApi.IHook.HookType.Action, hook) && hook.DoRedraw(args));
			}
			finally
			{
				ModEntry.Instance.ArgsPool.Return(args);
			}
		}
	}
	
	private static void Card_Render_Postfix(Card __instance, G g, Vec? posOverride, State? fakeState, double? overrideWidth, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		
		var state = fakeState ?? g.state;
		if (state.route is not Combat combat)
			return;
		if (combat.routeOverride is not null)
			return;
		if (!Instance.IsRedrawPossible(state, combat, __instance))
			return;

		var position = posOverride ?? __instance.pos;
		position += new Vec(0.0, __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim));
		position += new Vec(((overrideWidth ?? 59) - 21) / 2.0, 82 - 13 / 2.0 - 0.5);
		position = position.round();

		var result = SharedArt.ButtonSprite(
			g,
			new Rect(position.x, position.y, 19, 13),
			new UIKey((UK)21370099, __instance.uuid),
			(Spr)ModEntry.Instance.Content.RedrawButtonSprite.Id!.Value,
			(Spr)ModEntry.Instance.Content.RedrawButtonOnSprite.Id!.Value,
			onMouseDown: new MouseDownHandler(() => Instance.DoRedraw(state, combat, __instance))
		);
		if (result.isHover)
			g.tooltips.Add(position + new Vec(30, 10), ModEntry.Instance.Api.GetRedrawStatusTooltip());
	}
}

public sealed class StandardRedrawStatusPaymentHook : IKokoroApi.IV2.IRedrawStatusApi.IHook
{
	public static StandardRedrawStatusPaymentHook Instance { get; private set; } = new();

	private StandardRedrawStatusPaymentHook() { }

	public bool? CanRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.ICanRedrawArgs args)
		=> args.State.ship.Get((Status)ModEntry.Instance.Content.RedrawStatus.Id!.Value) > 0;

	public bool PayForRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IPayForRedrawArgs args)
	{
		args.State.ship.Add((Status)ModEntry.Instance.Content.RedrawStatus.Id!.Value, -1);
		return true;
	}
}

public sealed class StandardRedrawStatusActionHook : IKokoroApi.IV2.IRedrawStatusApi.IHook
{
	public static StandardRedrawStatusActionHook Instance { get; private set; } = new();

	private StandardRedrawStatusActionHook() { }

	public bool DoRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IDoRedrawArgs args)
	{
		if (!args.Combat.hand.Contains(args.Card))
			return false;

		args.State.RemoveCardFromWhereverItIs(args.Card.uuid);
		args.Card.OnDiscard(args.State, args.Combat);
		args.Combat.SendCardToDiscard(args.State, args.Card);
		args.Combat.DrawCards(args.State, 1);
		return true;
	}

	public void AfterRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IAfterRedrawArgs args)
		=> args.Combat.QueueImmediate(new ADummyAction { dialogueSelector = ".JustDidRedraw" });
}

internal sealed class V1ToV2RedrawStatusHookWrapper(IRedrawStatusHook v1) : IKokoroApi.IV2.IRedrawStatusApi.IHook
{
	public bool? CanRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.ICanRedrawArgs args)
		=> v1.CanRedraw(args.State, args.Combat, args.Card);

	public bool PayForRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IPayForRedrawArgs args)
		=> v1.PayForRedraw(args.State, args.Combat, args.Card, RedrawStatusManager.Instance.HookMapper.MapToV1(args.PossibilityHook));
	
	public bool DoRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IDoRedrawArgs args)
		=> v1.DoRedraw(args.State, args.Combat, args.Card, RedrawStatusManager.Instance.HookMapper.MapToV1(args.PossibilityHook), RedrawStatusManager.Instance.HookMapper.MapToV1(args.PaymentHook));
	
	public void AfterRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IAfterRedrawArgs args)
		=> v1.AfterRedraw(args.State, args.Combat, args.Card, RedrawStatusManager.Instance.HookMapper.MapToV1(args.PossibilityHook), RedrawStatusManager.Instance.HookMapper.MapToV1(args.PaymentHook), RedrawStatusManager.Instance.HookMapper.MapToV1(args.ActionHook));
}

internal sealed class V2ToV1RedrawStatusHookWrapper(IKokoroApi.IV2.IRedrawStatusApi.IHook v2) : IRedrawStatusHook
{
	public bool? CanRedraw(State state, Combat combat, Card card)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.CanRedrawArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			return v2.CanRedraw(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool PayForRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.PayForRedrawArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.PossibilityHook = RedrawStatusManager.Instance.HookMapper.MapToV2(possibilityHook);
			return v2.PayForRedraw(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool DoRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.DoRedrawArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.PossibilityHook = RedrawStatusManager.Instance.HookMapper.MapToV2(possibilityHook);
			args.PaymentHook = RedrawStatusManager.Instance.HookMapper.MapToV2(paymentHook);
			return v2.DoRedraw(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook, IRedrawStatusHook actionHook)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.RedrawStatusApi.AfterRedrawArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.PossibilityHook = RedrawStatusManager.Instance.HookMapper.MapToV2(possibilityHook);
			args.PaymentHook = RedrawStatusManager.Instance.HookMapper.MapToV2(paymentHook);
			args.ActionHook = RedrawStatusManager.Instance.HookMapper.MapToV2(actionHook);
			v2.AfterRedraw(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
}