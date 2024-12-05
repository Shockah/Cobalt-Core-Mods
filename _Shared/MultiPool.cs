using System;
using System.Collections.Generic;

namespace Shockah.Shared;

public sealed class MultiPool
{
	private readonly Dictionary<Type, object> Pools = [];

	public T Get<T>() where T : class, new()
		=> GetPool<T>().Get();

	public void Return<T>(T item) where T : class, new()
		=> GetPool<T>().Return(item);

	private Pool<T> GetPool<T>() where T : class, new()
	{
		if (!Pools.TryGetValue(typeof(T), out var rawPool))
		{
			rawPool = new Pool<T>(() => new T());
			Pools[typeof(T)] = rawPool;
		}
		return (Pool<T>)rawPool;
	}
}