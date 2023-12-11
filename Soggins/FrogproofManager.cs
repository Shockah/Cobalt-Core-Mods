using Shockah.Shared;

namespace Shockah.Soggins;

public sealed class FrogproofManager : HookManager<IFrogproofHook>
{
	internal FrogproofManager() : base()
	{
		Register(FrogproofCardTraitFrogproofHook.Instance, 0);
		Register(TrashCardFrogproofHook.Instance, -1);
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

public sealed class FrogproofCardTraitFrogproofHook : IFrogproofHook
{
	public static FrogproofCardTraitFrogproofHook Instance { get; private set; } = new();

	private FrogproofCardTraitFrogproofHook() { }

	public bool? IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> card is IFrogproofCard ? true : null;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class TrashCardFrogproofHook : IFrogproofHook
{
	public static TrashCardFrogproofHook Instance { get; private set; } = new();

	private TrashCardFrogproofHook() { }

	public bool? IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> card.GetMeta().deck == Deck.trash ? true : null;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}