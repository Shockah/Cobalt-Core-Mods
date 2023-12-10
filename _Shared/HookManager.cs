using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

public class HookManager<THook> : IEnumerable<THook> where THook : class
{
	private static ModEntry Instance => ModEntry.Instance;

	protected readonly OrderedList<THook, double> Hooks = new();

	public void Register(THook hook, double priority)
		=> Hooks.Add(hook, -priority);

	public void Unregister(THook hook)
		=> Hooks.Remove(hook);

	public IEnumerator<THook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public IEnumerable<THook> GetHooksWithProxies(IEnumerable<object> objects)
		=> Hooks
			.Select(hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? -priority : 0))
			.Concat(
				objects
					.Select(o => (Hook: Instance.Api.TryProxy<THook>(o, out var hook) ? hook : null, Priority: Instance.Api.TryProxy<IHookPriority>(o, out var hookPriority) ? hookPriority.HookPriority : 0))
					.Where(e => e.Hook is not null)
					.Select(e => (Hook: e.Hook!, Priority: e.Priority))
			)
			.OrderByDescending(e => e.Priority)
			.Select(e => e.Hook);
}