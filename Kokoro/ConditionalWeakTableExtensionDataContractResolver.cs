using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal sealed class ConditionalWeakTableExtensionDataContractResolver : IContractResolver
{
	private readonly IContractResolver Wrapped;
	private readonly string JsonKey;
	private readonly ExtensionDataManager Manager;

	private readonly Dictionary<Type, JsonContract> ContractCache = new();

	public ConditionalWeakTableExtensionDataContractResolver(IContractResolver wrapped, string jsonKey, ExtensionDataManager manager)
	{
		this.Wrapped = wrapped;
		this.JsonKey = jsonKey;
		this.Manager = manager;
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
		if (Manager.IsTypeRegisteredForExtensionData(o) && Manager.ExtensionDataStorage.TryGetValue(o, out var allObjectData))
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

		if (!Manager.IsTypeRegisteredForExtensionData(o))
			return;

		Manager.RegisterCloneListenerIfNeeded();

		if (value is null)
		{
			Manager.ExtensionDataStorage.Remove(o);
			return;
		}

		if (value is not Dictionary<string, Dictionary<string, object?>> dictionary)
		{
			ModEntry.Instance.Logger!.LogError("Encountered invalid serialized mod data of type {Type}.", value.GetType().FullName!);
			return;
		}

		Manager.ExtensionDataStorage.AddOrUpdate(o, dictionary);
	}
}
