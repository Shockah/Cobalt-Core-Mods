using Nanoray.Pintail;
using Nickel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Shared;

internal class HookManager<THook>(
	string proxyContext
) : IEnumerable<THook> where THook : class
{
	protected readonly OrderedList<THook, double> Hooks = new(ascending: false);
	private readonly Dictionary<Type, bool> HookTypes = [];
	private readonly Lazy<HashSet<string>> HookMethodNames = new(() => typeof(THook).GetMethods().Select(m => m.Name).ToHashSet());

	public void Register(THook hook, double priority)
		=> Hooks.Add(hook, priority);

	public void Unregister(THook hook)
		=> Hooks.Remove(hook);

	public IEnumerator<THook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	
	internal IEnumerable<THook> GetHooksWithProxies(IProxyManager<string> proxyManager, IEnumerable<object> objects)
		=> Hooks
			.Select(hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? priority : 0))
			.Concat(
				objects
					.Where(o => !HookTypes.TryGetValue(o.GetType(), out var isHookType) || isHookType)
					.Select<object, (THook? Hook, double Priority)>(o =>
					{
						while (o is IProxyObject.IWithProxyTargetInstanceProperty proxyObject)
							o = proxyObject.ProxyTargetInstance;
						var oType = o.GetType();
						var hookWasUnknownType = HookTypes.ContainsKey(oType);
						
						if (!oType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(m => HookMethodNames.Value.Contains(m.Name)))
						{
							HookTypes[oType] = false;
							return (Hook: null, Priority: 0);
						}
						
						if (!proxyManager.TryProxy(o, "AnyMod", proxyContext, out THook? hook))
						{
							HookTypes[oType] = false;
							return (Hook: null, Priority: 0);
						}
						
						if (hookWasUnknownType)
							HookTypes[oType] = true;
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