using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class RedrawStatusManager : HookManager<IRedrawStatusHook>
{
	internal RedrawStatusManager() : base()
	{
		Register(StandardRedrawStatusHook.Instance, 0);
	}

	public bool IsRedrawPossible(State state, Combat combat, Card card)
		=> GetHandlingHook(state, combat, card) is not null;

	public IRedrawStatusHook? GetHandlingHook(State state, Combat combat, Card card)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsRedrawPossible(state, combat, card);
			if (hookResult == false)
				return null;
			else if (hookResult == true)
				return hook;
		}
		return null;
	}

	public void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook hook)
	{
		foreach (var hooks in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			hooks.AfterRedraw(state, combat, card, hook);
	}
}

public sealed class StandardRedrawStatusHook : IRedrawStatusHook
{
	public static StandardRedrawStatusHook Instance { get; private set; } = new();

	private StandardRedrawStatusHook() { }

	public bool? IsRedrawPossible(State state, Combat combat, Card card)
		=> state.ship.Get((Status)ModEntry.Instance.Content.RedrawStatus.Id!.Value) > 0;

	public void PayForRedraw(State state, Combat combat, Card card)
		=> state.ship.Add((Status)ModEntry.Instance.Content.RedrawStatus.Id!.Value, -1);

	public void DoRedraw(State state, Combat combat, Card card)
	{
		if (!combat.hand.Contains(card))
			return;

		state.RemoveCardFromWhereverItIs(card.uuid);
		card.OnDiscard(state, combat);
		combat.SendCardToDiscard(state, card);
		combat.DrawCards(state, 1);
	}

	public void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook hook)
		=> combat.QueueImmediate(new ADummyAction { dialogueSelector = ".JustDidRedraw" });
}