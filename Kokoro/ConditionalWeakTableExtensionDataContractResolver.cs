using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

internal sealed class ConditionalWeakTableExtensionDataContractResolver : IContractResolver
{
	private readonly IContractResolver Wrapped;
	private readonly ConditionalWeakTable<object, Dictionary<string, object>> Storage = new();

	public ConditionalWeakTableExtensionDataContractResolver(IContractResolver wrapped, ConditionalWeakTable<object, Dictionary<string, object>> storage)
	{
		this.Wrapped = wrapped;
		this.Storage = storage;
	}

	public JsonContract ResolveContract(Type type)
	{
		var contract = Wrapped.ResolveContract(type);
		if (contract is JsonObjectContract objectContract)
		{
			objectContract.ExtensionDataGetter = ExtensionDataGetter;
			objectContract.ExtensionDataSetter = ExtensionDataSetter;
		}
		return contract;
	}

	private IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o)
	{
		if (!Storage.TryGetValue(o, out var data))
			yield break;
		foreach (var (key, value) in data)
			yield return new(key, value);
	}

	private void ExtensionDataSetter(object o, string key, object? value)
	{
		if (!Storage.TryGetValue(o, out var data))
		{
			data = new();
			Storage.AddOrUpdate(o, data);
		}
		if (value is null)
			data.Remove(key);
		else
			data[key] = value;
	}
}
