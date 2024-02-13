namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IEvadeHook VanillaEvadeHook { get; }
	IEvadeHook VanillaDebugEvadeHook { get; }
	void RegisterEvadeHook(IEvadeHook hook, double priority);
	void UnregisterEvadeHook(IEvadeHook hook);

	bool IsEvadePossible(State state, Combat combat, EvadeHookContext context);
	IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, EvadeHookContext context);
	void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook);
}

public enum EvadeHookContext
{
	Rendering, Action
}

public interface IEvadeHook
{
	bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context) => null;
	void PayForEvade(State state, Combat combat, int direction) { }
	void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook) { }
}