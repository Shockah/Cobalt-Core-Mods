using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

internal static class LinqExt
{
#if !IS_NICKEL_MOD
	public static T? FirstOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).FirstOrDefault();

	public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).FirstOrDefault();

	public static T? LastOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).LastOrDefault();

	public static T? LastOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).LastOrDefault();
#endif

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