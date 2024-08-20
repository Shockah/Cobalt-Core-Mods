using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

internal record struct WeightedItem<T>(
	double Weight,
	T Item
);

internal sealed class WeightedRandom<T>
{
	public IReadOnlyList<WeightedItem<T>> Items
		=> ItemStorage;

	public double WeightSum { get; private set; }

	private readonly List<WeightedItem<T>> ItemStorage = [];

	public WeightedRandom()
	{
	}

	public WeightedRandom(IEnumerable<WeightedItem<T>> items)
	{
		this.ItemStorage = items.ToList();
		this.WeightSum = this.ItemStorage.Sum(item => item.Weight);
	}

	public void Add(WeightedItem<T> item)
	{
		if (item.Weight <= 0)
			return;
		ItemStorage.Add(item);
		WeightSum += item.Weight;
	}

	public T Next(Rand random, bool consume = false)
	{
		switch (this.ItemStorage.Count)
		{
			case 0:
				throw new IndexOutOfRangeException("Cannot choose a random element, as the list is empty.");
			case 1:
			{
				var result = this.ItemStorage[0].Item;
				if (consume)
				{
					this.WeightSum = 0;
					this.ItemStorage.RemoveAt(0);
				}
				return result;
			}
		}

		var weightedRandom = random.Next() * WeightSum;
		for (var i = 0; i < ItemStorage.Count; i++)
		{
			var item = ItemStorage[i];
			weightedRandom -= item.Weight;

			if (weightedRandom <= 0)
			{
				if (consume)
				{
					WeightSum -= ItemStorage[i].Weight;
					ItemStorage.RemoveAt(i);
				}
				return item.Item;
			}
		}
		throw new InvalidOperationException("Invalid state.");
	}

	public IEnumerable<T> GetConsumingEnumerable(Rand random)
	{
		int count;
		while ((count = ItemStorage.Count) != 0)
		{
			if (count == 1)
			{
				var item = ItemStorage[0];
				WeightSum = 0;
				ItemStorage.RemoveAt(0);
				yield return item.Item;
				yield break;
			}
			
			var weightedRandom = random.Next() * this.WeightSum;
			for (var i = 0; i < this.ItemStorage.Count; i++)
			{
				var item = this.ItemStorage[i];
				weightedRandom -= item.Weight;

				if (weightedRandom <= 0)
				{
					this.WeightSum -= this.ItemStorage[i].Weight;
					this.ItemStorage.RemoveAt(i);
					yield return item.Item;
					break;
				}
			}
		}
	}
}

internal static class WeightedRandomClassExt
{
	public static T? NextOrNull<T>(this WeightedRandom<T> weightedRandom, Rand random, bool consume = false)
		where T : class
	{
		if (weightedRandom.Items.Count == 0)
			return null;
		return weightedRandom.Next(random, consume);
	}
}

internal static class WeightedRandomStructExt
{
	public static T? NextOrNull<T>(this WeightedRandom<T> weightedRandom, Rand random, bool consume = false)
		where T : struct
	{
		if (weightedRandom.Items.Count == 0)
			return null;
		return weightedRandom.Next(random, consume);
	}
}