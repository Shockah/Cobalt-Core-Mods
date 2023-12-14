using Shockah.Shared;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi : IProxyProvider
{
	TimeSpan TotalGameTime { get; }
	IEnumerable<Card> GetCardsEverywhere(State state, bool hand = true, bool drawPile = true, bool discardPile = true, bool exhaustPile = true);
}

public interface IHookPriority
{
	double HookPriority { get; }
}