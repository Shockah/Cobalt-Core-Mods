using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	ExternalStatus RedrawStatus { get; }
	Status RedrawVanillaStatus { get; }
	Tooltip GetRedrawStatusTooltip();

	void RegisterRedrawStatusHook(IRedrawStatusHook hook, double priority);
	void UnregisterRedrawStatusHook(IRedrawStatusHook hook);

	bool IsRedrawPossible(State state, Combat combat, Card card);
	IRedrawStatusHook? GetRedrawHandlingHook(State state, Combat combat, Card card);
	void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook hook);

	IRedrawStatusHook StandardRedrawStatusHook { get; }
}

public interface IRedrawStatusHook
{
	bool? IsRedrawPossible(State state, Combat combat, Card card) => null;
	void PayForRedraw(State state, Combat combat, Card card) { }
	void DoRedraw(State state, Combat combat, Card card) { }
	void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook hook) { }
}