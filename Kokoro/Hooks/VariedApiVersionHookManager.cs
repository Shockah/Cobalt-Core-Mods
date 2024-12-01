using Nanoray.Pintail;
using Nickel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

internal enum VariedApiVersionHookType
{
	None, V1, V2
}

internal class VariedApiVersionHookManager<TV2Hook, TV1Hook>(
	string proxyContext,
	IHookMapper<TV2Hook, TV1Hook> hookMapper
) : IEnumerable<TV2Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	protected readonly OrderedList<TV2Hook, double> Hooks = [];
	private readonly Dictionary<Type, VariedApiVersionHookType> HookTypes = [];
	private readonly Lazy<HashSet<string>> V2HookMethodNames = new(() => typeof(TV2Hook).GetMethods().Select(m => m.Name).ToHashSet());
	private readonly Lazy<HashSet<string>> V1HookMethodNames = new(() => typeof(TV1Hook).GetMethods().Select(m => m.Name).ToHashSet());

	public void Register(TV2Hook hook, double priority)
		=> Hooks.Add(hook, -priority);

	public void Unregister(TV2Hook hook)
		=> Hooks.Remove(hook);

	public void Register(TV1Hook hook, double priority)
		=> Register(hookMapper.MapToV2(hook), priority);

	public void Unregister(TV1Hook hook)
		=> Unregister(hookMapper.MapToV2(hook));

	public IEnumerator<TV2Hook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	
	internal IEnumerable<TV2Hook> GetHooksWithProxies(IProxyManager<string> proxyManager, IEnumerable<object> objects)
		=> Hooks
			.Select(hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? -priority : 0))
			.Concat(
				objects
					.Where(o => !HookTypes.TryGetValue(o.GetType(), out var hookType) || hookType != VariedApiVersionHookType.None)
					.Select<object, (TV2Hook? Hook, double Priority)>(o =>
					{
						while (o is IProxyObject.IWithProxyTargetInstanceProperty proxyObject)
							o = proxyObject.ProxyTargetInstance;
						var oType = o.GetType();

						var hookType = HookTypes.GetValueOrDefault(oType);
						var hookWasUnknownType = hookType == VariedApiVersionHookType.None;
						if (hookWasUnknownType)
							hookType = IsV2Hook(oType) ? VariedApiVersionHookType.V2 : VariedApiVersionHookType.V1;
						
						if (hookType == VariedApiVersionHookType.V2)
						{
							if (!oType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(m => V2HookMethodNames.Value.Contains(m.Name)))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}
							
							if (!proxyManager.TryProxy(o, "AnyMod", proxyContext, out TV2Hook? hook))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}

							if (hookWasUnknownType)
								HookTypes[oType] = VariedApiVersionHookType.V2;
							var priority = proxyManager.TryProxy(o, "AnyMod", proxyContext, out IKokoroApi.IV2.IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
							return (Hook: hook, Priority: priority);
						}
						else
						{
							if (!oType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(m => V1HookMethodNames.Value.Contains(m.Name)))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}
							
							if (!proxyManager.TryProxy(o, "AnyMod", proxyContext, out TV1Hook? hook))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}

							if (hookWasUnknownType)
								HookTypes[oType] = VariedApiVersionHookType.V1;
							var priority = proxyManager.TryProxy(o, "AnyMod", proxyContext, out IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
							return (Hook: hookMapper.MapToV2(hook), Priority: priority);
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