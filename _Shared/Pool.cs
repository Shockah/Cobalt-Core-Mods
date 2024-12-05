using System;
using System.Collections.Generic;

namespace Shockah.Shared;

public sealed class Pool<T>(
	Func<T> factory
) where T : class
{
	private readonly Queue<T> Queue = [];

	public T Get()
		=> Queue.Count == 0 ? factory() : Queue.Dequeue();
	
	public void Return(T @object)
		=> Queue.Enqueue(@object);
}