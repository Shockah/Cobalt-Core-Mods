using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

internal static class LinqExt
{
	public static int? FirstIndex<T>(this IList<T> self, Func<T, bool> predicate)
	{
		var index = 0;
		foreach (var item in self)
		{
			if (predicate(item))
				return index;
			index++;
		}
		return null;
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> self) where T : class
	{
		return self.OfType<T>();
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> self) where T : struct
	{
		foreach (var element in self)
			if (element is not null)
				yield return element.Value;
	}
}