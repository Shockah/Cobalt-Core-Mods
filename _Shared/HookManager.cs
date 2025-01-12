using Nanoray.Pintail;
using Nickel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Shared;

internal class HookManager<THook> : IEnumerable<THook> where THook : class
{
	protected readonly OrderedList<THook, double> Hooks = new(ascending: false);
	private readonly string ProxyContext;
	private readonly Dictionary<Type, bool> HookTypes = [];
	private readonly Lazy<HashSet<string>> HookMethodNames = new(() => typeof(THook).GetMethods().Select(m => m.Name).ToHashSet());

	private readonly Func<THook, (THook Hook, double Priority)> HookToHookWithPriorityDelegate;
	private readonly Func<object, bool> ObjectIsHookDelegate;
	private readonly Func<(THook? Hook, double Priority), bool> PotentialHookExistsDelegate;
	private readonly Func<(THook? Hook, double Priority), (THook Hook, double Priority)> PotentialHookToHookDelegate;
	private readonly Func<(THook Hook, double Priority), double> HookWithPriorityToPriorityDelegate;
	private readonly Func<(THook Hook, double Priority), THook> HookWithPriorityToHookDelegate;

	public HookManager(string proxyContext)
	{
		this.ProxyContext = proxyContext;

		this.HookToHookWithPriorityDelegate = hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? priority : 0);
		this.ObjectIsHookDelegate = o => !HookTypes.TryGetValue(o.GetType(), out var isHookType) || isHookType;
		this.PotentialHookExistsDelegate = e => e.Hook is not null;
		this.PotentialHookToHookDelegate = e => (Hook: e.Hook!, Priority: e.Priority);
		this.HookWithPriorityToPriorityDelegate = e => e.Priority;
		this.HookWithPriorityToHookDelegate = e => e.Hook;
	}

	public void Register(THook hook, double priority)
		=> Hooks.Add(hook, priority);

	public void Unregister(THook hook)
		=> Hooks.Remove(hook);

	public IEnumerator<THook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	internal IEnumerable<THook> GetHooksWithProxies(IProxyManager<string> proxyManager, IEnumerable<object> objects)
	{
		return Hooks
			.Select(this.HookToHookWithPriorityDelegate)
			.Concat(
				objects
					.Where(this.ObjectIsHookDelegate)
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

						if (!proxyManager.TryProxy(o, "AnyMod", this.ProxyContext, out THook? hook))
						{
							HookTypes[oType] = false;
							return (Hook: null, Priority: 0);
						}

						if (hookWasUnknownType)
							HookTypes[oType] = true;
						var priority = proxyManager.TryProxy(o, "AnyMod", this.ProxyContext, out IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
						return (Hook: hook, Priority: priority);
					})
					.Where(this.PotentialHookExistsDelegate)
					.Select(this.PotentialHookToHookDelegate)
			)
			.OrderByDescending(this.HookWithPriorityToPriorityDelegate)
			.Select(this.HookWithPriorityToHookDelegate);
	}
}

internal interface IHookPriority
{
	double HookPriority { get; }
}