using System;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	TimeSpan TotalGameTime { get; }

	bool TryProxy<T>(object @object, [MaybeNullWhen(false)] out T proxy) where T : class;
}

public interface IHookPriority
{
	double HookPriority { get; }
}