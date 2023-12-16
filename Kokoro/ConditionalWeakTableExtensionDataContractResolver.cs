using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

internal sealed class ConditionalWeakTableExtensionDataContractResolver : IContractResolver
{
	private readonly IContractResolver Wrapped;
	private readonly string JsonKey;
	private readonly ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object>>> Storage;
	private readonly Func<object, bool> ShouldStoreExtensionData;

	private readonly Dictionary<Type, JsonContract> ContractCache = new();

	public ConditionalWeakTableExtensionDataContractResolver(IContractResolver wrapped, string jsonKey, ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object>>> storage, Func<object, bool> shouldStoreExtensionData)
	{
		this.Wrapped = wrapped;
		this.JsonKey = jsonKey;
		this.Storage = storage;
		this.ShouldStoreExtensionData = shouldStoreExtensionData;
	}

	public JsonContract ResolveContract(Type type)
	{
		if (ContractCache.TryGetValue(type, out var contract))
			return contract;

		contract = Wrapped.ResolveContract(type);
		if (contract is JsonObjectContract objectContract)
		{
			var wrappedExtensionDataGetter = objectContract.ExtensionDataGetter;
			var wrappedExtensionDataSetter = objectContract.ExtensionDataSetter;
			objectContract.ExtensionDataGetter = o => ExtensionDataGetter(o, wrappedExtensionDataGetter);
			objectContract.ExtensionDataSetter = (o, key, value) => ExtensionDataSetter(o, key, value, wrappedExtensionDataSetter);
		}
		ContractCache[type] = contract;
		return contract;
	}

	private IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o, ExtensionDataGetter? wrapped)
	{
		if (o is null)
			yield break;
		if (ShouldStoreExtensionData(o) && Storage.TryGetValue(o, out var allObjectData))
			yield return new(JsonKey, allObjectData);
		if (wrapped is not null && wrapped.Invoke(o) is { } wrappedData)
			foreach (var kvp in wrappedData)
				if (!Equals(kvp.Key, JsonKey))
					yield return kvp;
	}

	private void ExtensionDataSetter(object o, string key, object? value, ExtensionDataSetter? wrapped)
	{
		if (key != JsonKey)
		{
			wrapped?.Invoke(o, key, value);
			return;
		}
		if (!ShouldStoreExtensionData(o))
			return;
		if (value is null)
		{
			Storage.Remove(o);
			return;
		}
		if (value is not Dictionary<string, Dictionary<string, object>> dictionary)
		{
			ModEntry.Instance.Logger!.LogError("Encountered invalid serialized mod data of type {Type}.", value.GetType().FullName!);
			return;
		}
		Storage.AddOrUpdate(o, dictionary);
	}
}
