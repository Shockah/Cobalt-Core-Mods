using Nickel;
using Shockah.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

internal class VariedApiVersionHookManager<TV2Hook, TV1Hook>(
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
	
	internal IEnumerable<TV2Hook> GetHooksWithProxies(IProxyProvider proxyProvider, IEnumerable<object> objects)
		=> Hooks
			.Select(hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? -priority : 0))
			.Concat(
				objects
					.Select(o =>
					{
						if (IsV2Hook(o.GetType()))
							return (Hook: proxyProvider.TryProxy<TV2Hook>(o, out var hook) ? hook : null, Priority: proxyProvider.TryProxy<IKokoroApi.IV2.IHookPriority>(o, out var hookPriority) ? hookPriority.HookPriority : 0);
						else
							return (Hook: proxyProvider.TryProxy<TV1Hook>(o, out var hook) ? mapper(hook) : null, Priority: proxyProvider.TryProxy<IHookPriority>(o, out var hookPriority) ? hookPriority.HookPriority : 0);

						static bool IsV2Hook(Type type)
							=> type == typeof(IKokoroApi.IV2.IKokoroV2ApiHook) || (type.BaseType is { } baseType && IsV2Hook(baseType)) || type.GetInterfaces().Any(IsV2Hook);
					})
					.Where(e => e.Hook is not null)
					.Select(e => (Hook: e.Hook!, Priority: e.Priority))
			)
			.OrderByDescending(e => e.Priority)
			.Select(e => e.Hook);
}