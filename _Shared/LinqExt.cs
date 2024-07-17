using System;
using System.Collections.Generic;

namespace Shockah.Shared;

internal static class LinqExt
{
	public static int? FirstIndex<T>(this IList<T> self, Func<T, bool> predicate)
	{
		int index = 0;
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
		foreach (var element in self)
			if (element is not null)
				yield return element;
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> self) where T : struct
	{
		foreach (var element in self)
			if (element is not null)
				yield return element.Value;
	}
}