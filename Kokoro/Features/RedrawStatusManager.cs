using Shockah.Shared;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class RedrawStatusManager : HookManager<IRedrawStatusHook>
{
	internal RedrawStatusManager()
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

		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			hook.AfterRedraw(state, combat, card, possibilityHook, paymentHook, actionHook);
		return true;

		IRedrawStatusHook? GetPossibilityHook()
		{
			foreach (var hook in this.GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
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
			=> this.GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts())
				.FirstOrDefault(hook => hook.PayForRedraw(state, combat, card, possibilityHook));

		IRedrawStatusHook? GetActionHook()
			=> this.GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts())
				.FirstOrDefault(hook => hook.DoRedraw(state, combat, card, possibilityHook, paymentHook));
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