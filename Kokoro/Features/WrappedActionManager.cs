using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class WrappedActionManager : HookManager<IWrappedActionHook>
{
	internal WrappedActionManager() : base()
	{
		Register(ConditionalActionWrappedActionHook.Instance, 0);
	}

	public IEnumerable<CardAction> GetWrappedCardActions(CardAction action)
	{
		foreach (var hook in Hooks)
		{
			var wrappedActions = hook.GetWrappedCardActions(action);
			if (wrappedActions is not null)
			{
				foreach (var wrappedAction in wrappedActions)
					yield return wrappedAction;
				yield break;
			}
		}
		yield return action;
	}

	public IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action)
	{
		foreach (var hook in Hooks)
		{
			var wrappedActions = hook.GetWrappedCardActions(action);
			if (wrappedActions is not null)
			{
				foreach (var wrappedAction in wrappedActions)
					foreach (var nestedWrappedAction in GetWrappedCardActionsRecursively(wrappedAction))
						yield return nestedWrappedAction;
				yield break;
			}
		}
		yield return action;
	}
}

public sealed class ConditionalActionWrappedActionHook : IWrappedActionHook
{
	public static ConditionalActionWrappedActionHook Instance { get; private set; } = new();

	private ConditionalActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AConditional conditional)
			return null;
		if (conditional.Action is not { } wrappedAction)
			return null;
		return new() { wrappedAction };
	}
}