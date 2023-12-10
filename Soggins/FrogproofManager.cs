using Shockah.Shared;

namespace Shockah.Soggins;

public sealed class FrogproofManager : HookManager<IFrogproofHook>
{
	internal FrogproofManager() : base()
	{
		Register(FrogproofCardTraintFrogproofHook.Instance, 0);
	}

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> GetHandlingHook(state, combat, card, context) is not null;

	public IFrogproofHook? GetHandlingHook(State state, Combat? combat, Card card, FrogproofHookContext context = FrogproofHookContext.Action)
	{
		foreach (var hook in Hooks)
		{
			var hookResult = hook.IsFrogproof(state, combat, card, context);
			if (hookResult == false)
				return null;
			else if (hookResult == true)
				return hook;
		}
		return null;
	}
}

public sealed class FrogproofCardTraintFrogproofHook : IFrogproofHook
{
	public static FrogproofCardTraintFrogproofHook Instance { get; private set; } = new();

	private FrogproofCardTraintFrogproofHook() { }

	public bool? IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> card is ApologyCard;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}