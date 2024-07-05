using System;

namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	TimeSpan TotalGameTime { get; }
}

public interface IHookPriority
{
	double HookPriority { get; }
}