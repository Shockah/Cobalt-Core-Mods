using System;
using System.Collections.Generic;

namespace Shockah.Natasha;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public interface IActionApi
	{
		CardAction MakeContinue(out Guid id);
		CardAction MakeContinued(Guid id, CardAction action);
		IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action);
		CardAction MakeStop(out Guid id);
		CardAction MakeStopped(Guid id, CardAction action);
		IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action);

		CardAction MakeSpoofed(CardAction renderAction, CardAction realAction);
		CardAction MakeHidden(CardAction action, bool showTooltips = false);
		AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer);
		AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null);
		AStatus MakeEnergy(AStatus action, bool energy = true);

		ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null);
		CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null);

		void RegisterWrappedActionHook(IWrappedActionHook hook, double priority);
		void UnregisterWrappedActionHook(IWrappedActionHook hook);
	}
}

public interface IWrappedActionHook
{
	List<CardAction>? GetWrappedCardActions(CardAction action);
}