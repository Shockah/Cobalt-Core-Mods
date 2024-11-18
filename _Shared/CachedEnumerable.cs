using System;
using System.Collections;
using System.Collections.Generic;

namespace Shockah.Shared;

internal static class CachedEnumerableExt
{
	public static CachedEnumerable<T> Cached<T>(this IEnumerable<T> enumerable)
		=> new CachedEnumerable<T>(enumerable);
}

internal sealed class CachedEnumerable<T>(IEnumerator<T> enumerator) : IEnumerable<T>, IDisposable
{
	private readonly List<T> Cache = [];
	
	public CachedEnumerable(IEnumerable<T> enumerable) : this(enumerable.GetEnumerator()) { }

	public IEnumerator<T> GetEnumerator()
	{
		var index = 0;
		while (true)
		{
			if (index < Cache.Count)
				yield return Cache[index++];
			else if (enumerator.MoveNext())
				Cache.Add(enumerator.Current);
			else
				yield break;
		}
	}

	public void Dispose()
		=> enumerator.Dispose();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
}