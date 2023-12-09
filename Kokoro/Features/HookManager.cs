using System.Collections;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public class HookManager<THook> : IEnumerable<THook>
{
	protected readonly OrderedList<THook, double> Hooks = new();

	public void Register(THook hook, double priority)
		=> Hooks.Add(hook, -priority);

	public void Unregister(THook hook)
		=> Hooks.Remove(hook);

	public IEnumerator<THook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> Hooks.GetEnumerator();
}