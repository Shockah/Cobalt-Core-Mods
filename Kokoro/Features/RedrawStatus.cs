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
		=> Kokoro.StandardRedrawStatusPaymentHook.Instance;

	public IRedrawStatusHook StandardRedrawStatusActionHook
		=> Kokoro.StandardRedrawStatusActionHook.Instance;
}

internal sealed class RedrawStatusManager : HookManager<IRedrawStatusHook>
{
	internal static readonly RedrawStatusManager Instance = new();
	
	public RedrawStatusManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
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
			if (hook.CanRedraw(state, combat, card) is { } result)
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
			hook.AfterRedraw(state, combat, card, possibilityHook, paymentHook, actionHook);
		return true;

		IRedrawStatusHook? GetPossibilityHook()
		{
			foreach (var hook in this.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			{
				switch (hook.CanRedraw(state, combat, card))
				{
					case false:
						return null;
					case true:
						return hook;
				}
			}
			return null;
		}

		IRedrawStatusHook? GetPaymentHook()
			=> this.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
				.FirstOrDefault(hook => hook.PayForRedraw(state, combat, card, possibilityHook));

		IRedrawStatusHook? GetActionHook()
			=> this.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
				.FirstOrDefault(hook => hook.DoRedraw(state, combat, card, possibilityHook, paymentHook));
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

public sealed class StandardRedrawStatusPaymentHook : IRedrawStatusHook
{
	public static StandardRedrawStatusPaymentHook Instance { get; private set; } = new();

	private StandardRedrawStatusPaymentHook() { }

	public bool? CanRedraw(State state, Combat combat, Card card)
		=> state.ship.Get((Status)ModEntry.Instance.Content.RedrawStatus.Id!.Value) > 0;

	public bool PayForRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook)
	{
		state.ship.Add((Status)ModEntry.Instance.Content.RedrawStatus.Id!.Value, -1);
		return true;
	}
}

public sealed class StandardRedrawStatusActionHook : IRedrawStatusHook
{
	public static StandardRedrawStatusActionHook Instance { get; private set; } = new();

	private StandardRedrawStatusActionHook() { }

	public bool DoRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook)
	{
		if (!combat.hand.Contains(card))
			return false;

		state.RemoveCardFromWhereverItIs(card.uuid);
		card.OnDiscard(state, combat);
		combat.SendCardToDiscard(state, card);
		combat.DrawCards(state, 1);
		return true;
	}

	public void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook, IRedrawStatusHook actionHook)
		=> combat.QueueImmediate(new ADummyAction { dialogueSelector = ".JustDidRedraw" });
}