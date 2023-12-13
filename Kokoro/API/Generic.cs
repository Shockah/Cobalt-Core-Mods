using Shockah.Shared;
using System;

namespace Shockah.Kokoro;

public partial interface IKokoroApi : IProxyProvider
{
	TimeSpan TotalGameTime { get; }
}

public interface IHookPriority
{
	double HookPriority { get; }
}