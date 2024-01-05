using CobaltCoreModding.Definitions.ModManifests;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

internal sealed class ExtensionDataManager
{
	internal readonly ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> ExtensionDataStorage = new();
	private readonly HashSet<Type> TypesRegisteredForExtensionData = new();

	private static T ConvertExtensionData<T>(object? o)
	{
		if (typeof(T).IsInstanceOfType(o))
			return (T)o;
		if (typeof(T) == typeof(int))
			return (T)(object)Convert.ToInt32(o);
		else if (typeof(T) == typeof(long))
			return (T)(object)Convert.ToInt64(o);
		else if (typeof(T) == typeof(short))
			return (T)(object)Convert.ToInt16(o);
		else if (typeof(T) == typeof(byte))
			return (T)(object)Convert.ToByte(o);
		else if (typeof(T) == typeof(bool))
			return (T)(object)Convert.ToBoolean(o);
		else if (typeof(T) == typeof(float))
			return (T)(object)Convert.ToSingle(o);
		else if (typeof(T) == typeof(double))
			return (T)(object)Convert.ToDouble(o);
		else if (o is null && (!typeof(T).IsValueType || (typeof(T).IsGenericType) && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)))
			return default!;
		else
			throw new ArgumentException($"Cannot convert {o} to extension data type {typeof(T)}", nameof(T));
	}

	internal bool IsTypeRegisteredForExtensionData(object o)
	{
		Type? currentType = o.GetType();
		while (true)
		{
			if (TypesRegisteredForExtensionData.Contains(currentType))
				return true;
			currentType = currentType.BaseType;
			if (currentType is null)
				break;
		}
		return false;
	}

	public void RegisterTypeForExtensionData(Type type)
		=> TypesRegisteredForExtensionData.Add(type);

	public T GetExtensionData<T>(IManifest manifest, object o, string key)
	{
		if (!IsTypeRegisteredForExtensionData(o))
			throw new InvalidOperationException($"Type {o.GetType().FullName} is not registered for storing extension data.");
		if (!ExtensionDataStorage.TryGetValue(o, out var allObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!allObjectData.TryGetValue(manifest.Name, out var modObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!modObjectData.TryGetValue(key, out var data))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		return ConvertExtensionData<T>(data);
	}

	public bool TryGetExtensionData<T>(IManifest manifest, object o, string key, [MaybeNullWhen(false)] out T data)
	{
		if (!IsTypeRegisteredForExtensionData(o))
			throw new InvalidOperationException($"Type {o.GetType().FullName} is not registered for storing extension data.");
		if (!ExtensionDataStorage.TryGetValue(o, out var allObjectData))
		{
			data = default;
			return false;
		}
		if (!allObjectData.TryGetValue(manifest.Name, out var modObjectData))
		{
			data = default;
			return false;
		}
		if (!modObjectData.TryGetValue(key, out var rawData))
		{
			data = default;
			return false;
		}
		data = ConvertExtensionData<T>(rawData);
		return true;
	}

	public T ObtainExtensionData<T>(IManifest manifest, object o, string key, Func<T> factory)
	{
		if (!TryGetExtensionData<T>(manifest, o, key, out var data))
		{
			data = factory();
			SetExtensionData(manifest, o, key, data);
		}
		return data;
	}

	public T ObtainExtensionData<T>(IManifest manifest, object o, string key) where T : new()
		=> ObtainExtensionData(manifest, o, key, () => new T());

	public bool ContainsExtensionData(IManifest manifest, object o, string key)
	{
		if (!IsTypeRegisteredForExtensionData(o))
			throw new InvalidOperationException($"Type {o.GetType().FullName} is not registered for storing extension data.");
		if (!ExtensionDataStorage.TryGetValue(o, out var allObjectData))
			return false;
		if (!allObjectData.TryGetValue(manifest.Name, out var modObjectData))
			return false;
		if (!modObjectData.TryGetValue(key, out _))
			return false;
		return true;
	}

	public void SetExtensionData<T>(IManifest manifest, object o, string key, T data)
	{
		if (!IsTypeRegisteredForExtensionData(o))
			throw new InvalidOperationException($"Type {o.GetType().FullName} is not registered for storing extension data.");
		if (!ExtensionDataStorage.TryGetValue(o, out var allObjectData))
		{
			allObjectData = new();
			ExtensionDataStorage.AddOrUpdate(o, allObjectData);
		}
		if (!allObjectData.TryGetValue(manifest.Name, out var modObjectData))
		{
			modObjectData = new();
			allObjectData[manifest.Name] = modObjectData;
		}
		modObjectData[key] = data;
	}

	public void RemoveExtensionData(IManifest manifest, object o, string key)
	{
		if (!IsTypeRegisteredForExtensionData(o))
			throw new InvalidOperationException($"Type {o.GetType().FullName} is not registered for storing extension data.");
		if (ExtensionDataStorage.TryGetValue(o, out var allObjectData))
		{
			if (allObjectData.TryGetValue(manifest.Name, out var modObjectData))
			{
				modObjectData.Remove(key);
				if (modObjectData.Count == 0)
					allObjectData.Remove(manifest.Name);
			}
			if (allObjectData.Count == 0)
				ExtensionDataStorage.Remove(o);
		}
	}
}