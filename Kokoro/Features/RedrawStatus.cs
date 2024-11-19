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
		=> RedrawStatusManager.Instance.Register(hook, priority);

	public void UnregisterRedrawStatusHook(IRedrawStatusHook hook)
		=> RedrawStatusManager.Instance.Unregister(hook);

	public bool IsRedrawPossible(State state, Combat combat, Card card)
		=> RedrawStatusManager.Instance.IsRedrawPossible(state, combat, card);

	public bool DoRedraw(State state, Combat combat, Card card)
		=> RedrawStatusManager.Instance.DoRedraw(state, combat, card);

	public IRedrawStatusHook StandardRedrawStatusPaymentHook
		=> new V2ToV1RedrawStatusHookWrapper(Kokoro.StandardRedrawStatusPaymentHook.Instance);

	public IRedrawStatusHook StandardRedrawStatusActionHook
		=> new V2ToV1RedrawStatusHookWrapper(Kokoro.StandardRedrawStatusActionHook.Instance);
	
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
			{
				throw new NotImplementedException();
			}

			public bool DoRedraw(State state, Combat combat, Card card)
			{
				throw new NotImplementedException();
			}

			public void RegisterHook(IKokoroApi.IV2.IRedrawStatusApi.IHook hook, double priority = 0)
				=> RedrawStatusManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IRedrawStatusApi.IHook hook)
				=> RedrawStatusManager.Instance.Unregister(hook);
			
			internal record struct CanRedrawArgs(
				State State,
				Combat Combat,
				Card Card
			) : IKokoroApi.IV2.IRedrawStatusApi.IHook.ICanRedrawArgs;
			
			internal record struct PayForRedrawArgs(
				State State,
				Combat Combat,
				Card Card,
				IKokoroApi.IV2.IRedrawStatusApi.IHook PossibilityHook
			) : IKokoroApi.IV2.IRedrawStatusApi.IHook.IPayForRedrawArgs;
			
			internal record struct DoRedrawArgs(
				State State,
				Combat Combat,
				Card Card,
				IKokoroApi.IV2.IRedrawStatusApi.IHook PossibilityHook,
				IKokoroApi.IV2.IRedrawStatusApi.IHook PaymentHook
			) : IKokoroApi.IV2.IRedrawStatusApi.IHook.IDoRedrawArgs;
			
			internal record struct AfterRedrawArgs(
				State State,
				Combat Combat,
				Card Card,
				IKokoroApi.IV2.IRedrawStatusApi.IHook PossibilityHook,
				IKokoroApi.IV2.IRedrawStatusApi.IHook PaymentHook,
				IKokoroApi.IV2.IRedrawStatusApi.IHook ActionHook
			) : IKokoroApi.IV2.IRedrawStatusApi.IHook.IAfterRedrawArgs;
		}
	}
	
	internal record struct ModifyOxidationRequirementArgs(
		State State,
		Ship Ship,
		int Value
	) : IKokoroApi.IV2.IOxidationStatusApi.IHook.IModifyOxidationRequirementArgs;
}

internal sealed class RedrawStatusManager : VariedApiVersionHookManager<IKokoroApi.IV2.IRedrawStatusApi.IHook, IRedrawStatusHook>
{
	internal static readonly RedrawStatusManager Instance = new();
	
	public RedrawStatusManager() : base(ModEntry.Instance.Package.Manifest.UniqueName, hook => new V1ToV2RedrawStatusHookWrapper(hook))
	{
		Register(StandardRedrawStatusPaymentHook.Instance, 0);
		Register(StandardRedrawStatusActionHook.Instance, -1000);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix))
		);
	}

	public bool IsRedrawPossible(State state, Combat combat, Card card)
	{
		if (!combat.isPlayerTurn)
			return false;
		if (!combat.hand.Contains(card))
			return false;

		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			if (hook.CanRedraw(new ApiImplementation.V2Api.RedrawStatusApi.CanRedrawArgs(state, combat, card)) is { } result)
				return result;
		return false;
	}

	public bool DoRedraw(State state, Combat combat, Card card)
	{
		if (GetPossibilityHook() is not { } possibilityHook)
			return false;
		if (GetPaymentHook() is not { } paymentHook)
			return false;
		if (GetActionHook() is not { } actionHook)
			return false;

		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			hook.AfterRedraw(new ApiImplementation.V2Api.RedrawStatusApi.AfterRedrawArgs(state, combat, card, possibilityHook, paymentHook, actionHook));
		return true;

		IKokoroApi.IV2.IRedrawStatusApi.IHook? GetPossibilityHook()
		{
			foreach (var hook in this.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			{
				switch (hook.CanRedraw(new ApiImplementation.V2Api.RedrawStatusApi.CanRedrawArgs(state, combat, card)))
				{
					case false:
						return null;
					case true:
						return hook;
				}
			}
			return null;
		}

		IKokoroApi.IV2.IRedrawStatusApi.IHook? GetPaymentHook()
			=> this.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
				.FirstOrDefault(hook => hook.PayForRedraw(new ApiImplementation.V2Api.RedrawStatusApi.PayForRedrawArgs(state, combat, card, possibilityHook)));

		IKokoroApi.IV2.IRedrawStatusApi.IHook? GetActionHook()
			=> this.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
				.FirstOrDefault(hook => hook.DoRedraw(new ApiImplementation.V2Api.RedrawStatusApi.DoRedrawArgs(state, combat, card, possibilityHook, paymentHook)));
	}
	
	private static void Card_Render_Postfix(Card __instance, G g, Vec? posOverride, State? fakeState, double? overrideWidth)
	{
		var state = fakeState ?? g.state;
		if (state.route is not Combat combat)
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
		=> v1.PayForRedraw(args.State, args.Combat, args.Card, new V2ToV1RedrawStatusHookWrapper(args.PossibilityHook));
	
	public bool DoRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IDoRedrawArgs args)
		=> v1.DoRedraw(args.State, args.Combat, args.Card, new V2ToV1RedrawStatusHookWrapper(args.PossibilityHook), new V2ToV1RedrawStatusHookWrapper(args.PaymentHook));
	
	public void AfterRedraw(IKokoroApi.IV2.IRedrawStatusApi.IHook.IAfterRedrawArgs args)
		=> v1.AfterRedraw(args.State, args.Combat, args.Card, new V2ToV1RedrawStatusHookWrapper(args.PossibilityHook), new V2ToV1RedrawStatusHookWrapper(args.PaymentHook), new V2ToV1RedrawStatusHookWrapper(args.ActionHook));
}

internal sealed class V2ToV1RedrawStatusHookWrapper(IKokoroApi.IV2.IRedrawStatusApi.IHook v2) : IRedrawStatusHook
{
	public bool? CanRedraw(State state, Combat combat, Card card)
		=> v2.CanRedraw(new ApiImplementation.V2Api.RedrawStatusApi.CanRedrawArgs(state, combat, card));
	
	public bool PayForRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook)
		=> v2.PayForRedraw(new ApiImplementation.V2Api.RedrawStatusApi.PayForRedrawArgs(state, combat, card, new V1ToV2RedrawStatusHookWrapper(possibilityHook)));
	
	public bool DoRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook)
		=> v2.DoRedraw(new ApiImplementation.V2Api.RedrawStatusApi.DoRedrawArgs(state, combat, card, new V1ToV2RedrawStatusHookWrapper(possibilityHook), new V1ToV2RedrawStatusHookWrapper(paymentHook)));
	
	public void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook, IRedrawStatusHook actionHook)
		=> v2.AfterRedraw(new ApiImplementation.V2Api.RedrawStatusApi.AfterRedrawArgs(state, combat, card, new V1ToV2RedrawStatusHookWrapper(possibilityHook), new V1ToV2RedrawStatusHookWrapper(paymentHook), new V1ToV2RedrawStatusHookWrapper(actionHook)));
}