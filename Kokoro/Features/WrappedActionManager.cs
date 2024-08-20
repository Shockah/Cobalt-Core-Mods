using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class WrappedActionManager : HookManager<IWrappedActionHook>
{
	internal WrappedActionManager()
	{
		Register(ConditionalActionWrappedActionHook.Instance, 0);
		Register(ResourceCostActionWrappedActionHook.Instance, 0);
		Register(ContinuedActionWrappedActionHook.Instance, 0);
		Register(HiddenActionWrappedActionHook.Instance, 0);
		Register(SpoofedActionWrappedActionHook.Instance, 0);
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

	public IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
	{
		foreach (var hook in Hooks)
		{
			var wrappedActions = hook.GetWrappedCardActions(action);
			if (wrappedActions is not null)
			{
				foreach (var wrappedAction in wrappedActions)
					foreach (var nestedWrappedAction in GetWrappedCardActionsRecursively(wrappedAction, includingWrapperActions))
						yield return nestedWrappedAction;
				if (includingWrapperActions)
					yield return action;
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
		return [wrappedAction];
	}
}

public sealed class ResourceCostActionWrappedActionHook : IWrappedActionHook
{
	public static ResourceCostActionWrappedActionHook Instance { get; private set; } = new();

	private ResourceCostActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AResourceCost resourceCostAction)
			return null;
		if (resourceCostAction.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class ContinuedActionWrappedActionHook : IWrappedActionHook
{
	public static ContinuedActionWrappedActionHook Instance { get; private set; } = new();

	private ContinuedActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AContinued continued)
			return null;
		if (continued.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class HiddenActionWrappedActionHook : IWrappedActionHook
{
	public static HiddenActionWrappedActionHook Instance { get; private set; } = new();

	private HiddenActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AHidden hidden)
			return null;
		if (hidden.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class SpoofedActionWrappedActionHook : IWrappedActionHook
{
	public static SpoofedActionWrappedActionHook Instance { get; private set; } = new();

	private SpoofedActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not ASpoofed spoofed)
			return null;

		List<CardAction> results = [];
		if (spoofed.RenderAction is { } renderAction)
			results.Add(renderAction);
		if (spoofed.RealAction is { } realAction)
			results.Add(realAction);
		return results.Count == 0 ? null : results;
	}
}