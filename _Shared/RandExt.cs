using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

internal static class RandExt
{
	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Rand rng)
		=> source.OrderBy(item => rng.NextInt());
}