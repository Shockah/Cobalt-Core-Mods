using Nanoray.Pintail;
using Nickel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Shared;

internal class HookManager<THook>(
	string proxyContext
) : IEnumerable<THook> where THook : class
{
	protected readonly OrderedList<THook, double> Hooks = [];

	public void Register(THook hook, double priority)
		=> Hooks.Add(hook, -priority);

	public void Unregister(THook hook)
		=> Hooks.Remove(hook);

	public IEnumerator<THook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	
	internal IEnumerable<THook> GetHooksWithProxies(IProxyManager<string> proxyManager, IEnumerable<object> objects)
		=> Hooks
			.Select(hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? -priority : 0))
			.Concat(
				objects
					.Select<object, (THook? Hook, double Priority)>(o =>
					{
						if (!proxyManager.TryProxy(o, "AnyMod", proxyContext, out THook? hook))
							return (Hook: null, Priority: 0);

						var priority = proxyManager.TryProxy(o, "AnyMod", proxyContext, out IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
						return (Hook: hook, Priority: priority);
					})
					.Where(e => e.Hook is not null)
					.Select(e => (Hook: e.Hook!, Priority: e.Priority))
			)
			.OrderByDescending(e => e.Priority)
			.Select(e => e.Hook);
}

internal interface IHookPriority
{
	double HookPriority { get; }
}