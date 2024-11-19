using Nanoray.Pintail;
using Nickel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

internal class VariedApiVersionHookManager<TV2Hook, TV1Hook>(
	string proxyContext,
	Func<TV1Hook, TV2Hook> mapper
) : IEnumerable<TV2Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	protected readonly OrderedList<TV2Hook, double> Hooks = [];
	private readonly ConditionalWeakTable<TV1Hook, TV2Hook> V1ToV2MappedHooks = [];

	public void Register(TV2Hook hook, double priority)
		=> Hooks.Add(hook, -priority);

	public void Unregister(TV2Hook hook)
		=> Hooks.Remove(hook);

	public void Register(TV1Hook hook, double priority)
		=> Register(V1ToV2MappedHooks.GetValue(hook, key => mapper(key)), priority);

	public void Unregister(TV1Hook hook)
	{
		if (V1ToV2MappedHooks.TryGetValue(hook, out var v2Hook))
			Unregister(v2Hook);
	}

	public IEnumerator<TV2Hook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	
	internal IEnumerable<TV2Hook> GetHooksWithProxies(IProxyManager<string> proxyManager, IEnumerable<object> objects)
		=> Hooks
			.Select(hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? -priority : 0))
			.Concat(
				objects
					.Select(o =>
					{
						if (IsV2Hook(o.GetType()))
						{
							if (!proxyManager.TryProxy(o, "AnyMod", proxyContext, out TV2Hook? hook))
								return (Hook: null, Priority: 0);

							var priority = proxyManager.TryProxy(o, "AnyMod", proxyContext, out IKokoroApi.IV2.IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
							return (Hook: hook, Priority: priority);
						}
						else
						{
							if (!proxyManager.TryProxy(o, "AnyMod", proxyContext, out TV1Hook? hook))
								return (Hook: null, Priority: 0);

							var priority = proxyManager.TryProxy(o, "AnyMod", proxyContext, out IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
							return (Hook: mapper(hook), Priority: priority);
						}

						static bool IsV2Hook(Type type)
							=> type.Name == nameof(IKokoroApi.IV2.IKokoroV2ApiHook) || (type.BaseType is { } baseType && IsV2Hook(baseType)) || type.GetInterfaces().Any(IsV2Hook);
					})
					.Where(e => e.Hook is not null)
					.Select(e => (Hook: e.Hook!, Priority: e.Priority))
			)
			.OrderByDescending(e => e.Priority)
			.Select(e => e.Hook);
}