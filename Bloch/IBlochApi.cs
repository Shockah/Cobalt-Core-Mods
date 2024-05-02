using Nickel;
using System.Collections.Generic;

namespace Shockah.Bloch;

public interface IBlochApi
{
	IDeckEntry BlochDeck { get; }

	void RegisterHook(IBlochHook hook, double priority);
	void UnregisterHook(IBlochHook hook);
}

public interface IBlochHook
{
	void OnScryResult(State state, Combat combat, IReadOnlyList<Card> presentedCards, IReadOnlyList<Card> discardedCards, bool fromInsight) { }
}