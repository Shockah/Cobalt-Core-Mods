using System;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

internal interface IHookMapper<out TV2Hook, in TV1Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	TV2Hook MapToV2(TV1Hook hook);
}

internal interface IBidirectionalHookMapper<TV2Hook, TV1Hook> : IHookMapper<TV2Hook, TV1Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	TV1Hook MapToV1(TV2Hook hook);
}

internal sealed class HookMapper<TV2Hook, TV1Hook>(
	Func<TV1Hook, TV2Hook> v1ToV2Mapper
) : IHookMapper<TV2Hook, TV1Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	private readonly ConditionalWeakTable<TV1Hook, TV2Hook> V1ToV2MappedHooks = [];

	public TV2Hook MapToV2(TV1Hook hook)
	{
		if (V1ToV2MappedHooks.TryGetValue(hook, out var result))
			return result;
		
		result = v1ToV2Mapper(hook);
		V1ToV2MappedHooks.AddOrUpdate(hook, result);
		return result;
	}
}

internal sealed class BidirectionalHookMapper<TV2Hook, TV1Hook>(
	Func<TV1Hook, TV2Hook> v1ToV2Mapper,
	Func<TV2Hook, TV1Hook> v2ToV1Mapper
) : IBidirectionalHookMapper<TV2Hook, TV1Hook>
	where TV2Hook : class
	where TV1Hook : class
{
	private readonly ConditionalWeakTable<TV1Hook, TV2Hook> V1ToV2MappedHooks = [];
	private readonly ConditionalWeakTable<TV2Hook, TV1Hook> V2ToV1MappedHooks = [];

	public TV2Hook MapToV2(TV1Hook hook)
	{
		if (V1ToV2MappedHooks.TryGetValue(hook, out var result))
			return result;
		
		result = v1ToV2Mapper(hook);
		V1ToV2MappedHooks.AddOrUpdate(hook, result);
		V2ToV1MappedHooks.AddOrUpdate(result, hook);
		return result;
	}

	public TV1Hook MapToV1(TV2Hook hook)
	{
		if (V2ToV1MappedHooks.TryGetValue(hook, out var result))
			return result;
		
		result = v2ToV1Mapper(hook);
		V2ToV1MappedHooks.AddOrUpdate(hook, result);
		V1ToV2MappedHooks.AddOrUpdate(result, hook);
		return result;
	}
}