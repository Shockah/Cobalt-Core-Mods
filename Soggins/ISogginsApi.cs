namespace Shockah.Soggins;

public interface ISogginsApi
{
	bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context);
	void RegisterFrogproofHook(IFrogproofHook hook, double priority);
	void UnregisterFrogproofHook(IFrogproofHook hook);
}

public enum FrogproofHookContext
{
	Rendering, Action
}

public interface IFrogproofHook
{
	bool? IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context);
	void PayForFrogproof(State state, Combat? combat, Card card);
}