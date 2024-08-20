using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

internal static class RandExt
{
	public static bool Chance(this Rand random, double chance)
		=> chance > 0 && (chance >= 1 || random.Next() < chance);

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Rand rng)
		=> source.OrderBy(_ => rng.NextInt());
}