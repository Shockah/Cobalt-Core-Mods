using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class RedrawStatusManager : HookManager<IRedrawStatusHook>
{
	internal RedrawStatusManager() : base()
	{
		Register(StandardRedrawStatusPaymentHook.Instance, 0);
		Register(StandardRedrawStatusActionHook.Instance, -1000);
	}

	public bool IsRedrawPossible(State state, Combat combat, Card card)
	{
		if (!combat.isPlayerTurn)
			return false;
		if (!combat.hand.Contains(card))
			return false;

		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.CanRedraw(state, combat, card);
			if (hookResult == false)
				return false;
			else if (hookResult == true)
				return true;
		}
		return false;
	}

	public bool DoRedraw(State state, Combat combat, Card card)
	{
		IRedrawStatusHook? GetPossibilityHook()
		{
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			{
				var hookResult = hook.CanRedraw(state, combat, card);
				if (hookResult == false)
					return null;
				else if (hookResult == true)
					return hook;
			}
			return null;
		}

		if (GetPossibilityHook() is not { } possibilityHook)
			return false;

		IRedrawStatusHook? GetPaymentHook()
		{
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
				if (hook.PayForRedraw(state, combat, card, possibilityHook))
					return hook;
			return null;
		}

		if (GetPaymentHook() is not { } paymentHook)
			return false;

		IRedrawStatusHook? GetActionHook()
		{
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
				if (hook.DoRedraw(state, combat, card, possibilityHook, paymentHook))
					return hook;
			return null;
		}

		if (GetActionHook() is not { } actionHook)
			return false;

		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			hook.AfterRedraw(state, combat, card, possibilityHook, paymentHook, actionHook);
		return true;
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