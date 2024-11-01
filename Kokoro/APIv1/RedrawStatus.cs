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
	bool DoRedraw(State state, Combat combat, Card card);

	IRedrawStatusHook StandardRedrawStatusPaymentHook { get; }
	IRedrawStatusHook StandardRedrawStatusActionHook { get; }
}

public interface IRedrawStatusHook
{
	bool? CanRedraw(State state, Combat combat, Card card) => null;
	bool PayForRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook) => false;
	bool DoRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook) => false;
	void AfterRedraw(State state, Combat combat, Card card, IRedrawStatusHook possibilityHook, IRedrawStatusHook paymentHook, IRedrawStatusHook actionHook) { }
}