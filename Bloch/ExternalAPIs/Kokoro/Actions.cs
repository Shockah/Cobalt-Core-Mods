using System;
using System.Collections.Generic;

namespace Shockah.Bloch;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public partial interface IActionApi
	{
		CardAction MakeExhaustEntireHandImmediate();

		CardAction MakeContinue(out Guid id);
		CardAction MakeContinued(Guid id, CardAction action);
		IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action);
		CardAction MakeStop(out Guid id);
		CardAction MakeStopped(Guid id, CardAction action);
		IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action);

		CardAction MakeHidden(CardAction action, bool showTooltips = false);
		AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer);
		AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null);
		AStatus MakeEnergy(AStatus action, bool energy = true);

		void RegisterWrappedActionHook(IWrappedActionHook hook, double priority);
		void UnregisterWrappedActionHook(IWrappedActionHook hook);
		
		CardAction MakeOnTurnEndAction(CardAction action);
	}
}

public interface IWrappedActionHook
{
	List<CardAction>? GetWrappedCardActions(CardAction action);
}