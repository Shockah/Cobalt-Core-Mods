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

internal class VariedApiVersionHookManager<TV2Hook, TV1Hook> : IEnumerable<TV2Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	protected readonly OrderedList<TV2Hook, double> Hooks = new(ascending: false);
	private readonly string ProxyContext;
	private readonly IHookMapper<TV2Hook, TV1Hook> HookMapper;
	private readonly Dictionary<Type, VariedApiVersionHookType> HookTypes = [];
	private readonly Lazy<HashSet<string>> V2HookMethodNames = new(() => typeof(TV2Hook).GetMethods().Select(m => m.Name).ToHashSet());
	private readonly Lazy<HashSet<string>> V1HookMethodNames = new(() => typeof(TV1Hook).GetMethods().Select(m => m.Name).ToHashSet());
	
	private readonly Func<TV2Hook, (TV2Hook Hook, double Priority)> HookToHookWithPriorityDelegate;
	private readonly Func<object, bool> ObjectIsHookDelegate;
	private readonly Func<MethodInfo, bool> IsV2HookMethodDelegate;
	private readonly Func<MethodInfo, bool> IsV1HookMethodDelegate;
	private readonly Func<(TV2Hook? Hook, double Priority), bool> PotentialHookExistsDelegate;
	private readonly Func<(TV2Hook? Hook, double Priority), (TV2Hook Hook, double Priority)> PotentialHookToHookDelegate;
	private readonly Func<(TV2Hook Hook, double Priority), double> HookWithPriorityToPriorityDelegate;
	private readonly Func<(TV2Hook Hook, double Priority), TV2Hook> HookWithPriorityToHookDelegate;
	
	public VariedApiVersionHookManager(string proxyContext, IHookMapper<TV2Hook, TV1Hook> hookMapper)
	{
		this.ProxyContext = proxyContext;
		this.HookMapper = hookMapper;

		this.HookToHookWithPriorityDelegate = hook => (Hook: hook, Priority: Hooks.TryGetOrderingValue(hook, out var priority) ? priority : 0);
		this.ObjectIsHookDelegate = o => !HookTypes.TryGetValue(o.GetType(), out var hookType) || hookType != VariedApiVersionHookType.None;
		this.IsV2HookMethodDelegate = m => V2HookMethodNames.Value.Contains(m.Name);
		this.IsV1HookMethodDelegate = m => V1HookMethodNames.Value.Contains(m.Name);
		this.PotentialHookExistsDelegate = e => e.Hook is not null;
		this.PotentialHookToHookDelegate = e => (Hook: e.Hook!, Priority: e.Priority);
		this.HookWithPriorityToPriorityDelegate = e => e.Priority;
		this.HookWithPriorityToHookDelegate = e => e.Hook;
	}

	public void Register(TV2Hook hook, double priority)
		=> Hooks.Add(hook, priority);

	public void Unregister(TV2Hook hook)
		=> Hooks.Remove(hook);

	public void Register(TV1Hook hook, double priority)
		=> Register(this.HookMapper.MapToV2(hook), priority);

	public void Unregister(TV1Hook hook)
		=> Unregister(this.HookMapper.MapToV2(hook));

	public IEnumerator<TV2Hook> GetEnumerator()
		=> Hooks.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	
	internal IEnumerable<TV2Hook> GetHooksWithProxies(IProxyManager<string> proxyManager, IEnumerable<object> objects)
		=> Hooks
			.Select(this.HookToHookWithPriorityDelegate)
			.Concat(
				objects
					.Where(this.ObjectIsHookDelegate)
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
							if (!oType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(this.IsV2HookMethodDelegate))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}
							
							if (!proxyManager.TryProxy(o, "AnyMod", this.ProxyContext, out TV2Hook? hook))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}

							if (hookWasUnknownType)
								HookTypes[oType] = VariedApiVersionHookType.V2;
							var priority = proxyManager.TryProxy(o, "AnyMod", this.ProxyContext, out IKokoroApi.IV2.IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
							return (Hook: hook, Priority: priority);
						}
						else
						{
							if (!oType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(this.IsV1HookMethodDelegate))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}
							
							if (!proxyManager.TryProxy(o, "AnyMod", this.ProxyContext, out TV1Hook? hook))
							{
								HookTypes[oType] = VariedApiVersionHookType.None;
								return (Hook: null, Priority: 0);
							}

							if (hookWasUnknownType)
								HookTypes[oType] = VariedApiVersionHookType.V1;
							var priority = proxyManager.TryProxy(o, "AnyMod", this.ProxyContext, out IHookPriority? hookPriority) ? hookPriority.HookPriority : 0;
							return (Hook: this.HookMapper.MapToV2(hook), Priority: priority);
						}

						static bool IsV2Hook(Type type)
							=> type.Name == nameof(IKokoroApi.IV2.IKokoroV2ApiHook) || (type.BaseType is { } baseType && IsV2Hook(baseType)) || type.GetInterfaces().Any(IsV2Hook);
					})
					.Where(this.PotentialHookExistsDelegate)
					.Select(this.PotentialHookToHookDelegate)
			)
			.OrderByDescending(this.HookWithPriorityToPriorityDelegate)
			.Select(this.HookWithPriorityToHookDelegate);
}