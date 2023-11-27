using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class OrderedList<TElement, TOrderingValue> : IReadOnlyList<TElement> where TOrderingValue : IComparable<TOrderingValue>
{
	private record struct Entry(
		TElement Element,
		TOrderingValue OrderingValue
	);

	private readonly List<Entry> Entries = new();

	public int Count
		=> Entries.Count;

	public TElement this[int index]
		=> Entries[index].Element;

	public IEnumerator<TElement> GetEnumerator()
		=> Entries.Select(e => e.Element).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public void Clear()
		=> Entries.Clear();

	public bool Contains(TElement item)
		=> Entries.Any(e => Equals(e.Element, item));

	public void Add(TElement element, TOrderingValue orderingValue)
	{
		for (int i = 0; i < Entries.Count; i++)
		{
			if (Entries[i].OrderingValue.CompareTo(orderingValue) > 0)
			{
				Entries.Insert(i, new(element, orderingValue));
				return;
			}
		}
		Entries.Add(new(element, orderingValue));
	}

	public bool Remove(TElement element)
	{
		for (int i = 0; i < Entries.Count; i++)
		{
			if (Equals(Entries[i].Element, element))
			{
				Entries.RemoveAt(i);
				return true;
			}
		}
		return false;
	}
}