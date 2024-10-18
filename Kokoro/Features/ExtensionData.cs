using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Nanoray.Mitosis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public void RegisterTypeForExtensionData(Type type)
	{
	}

	public T GetExtensionData<T>(object o, string key)
		=> Instance.ExtensionDataManager.GetExtensionData<T>(Manifest, o, key);

	public bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> Instance.ExtensionDataManager.TryGetExtensionData(Manifest, o, key, out data);

	public T ObtainExtensionData<T>(object o, string key, Func<T> factory)
		=> Instance.ExtensionDataManager.ObtainExtensionData(Manifest, o, key, factory);

	public T ObtainExtensionData<T>(object o, string key) where T : new()
		=> Instance.ExtensionDataManager.ObtainExtensionData<T>(Manifest, o, key);

	public bool ContainsExtensionData(object o, string key)
		=> Instance.ExtensionDataManager.ContainsExtensionData(Manifest, o, key);

	public void SetExtensionData<T>(object o, string key, T data)
		=> Instance.ExtensionDataManager.SetExtensionData(Manifest, o, key, data);

	public void RemoveExtensionData(object o, string key)
		=> Instance.ExtensionDataManager.RemoveExtensionData(Manifest, o, key);
}

internal sealed class ExtensionDataManager : IReferenceCloneListener
{
	internal readonly ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> ExtensionDataStorage = [];
	private bool IsRegisteredCloneListener;

	private static T ConvertExtensionData<T>(object? o)
	{
		if (o is T t)
			return t;
		if (typeof(T) == typeof(int))
			return (T)(object)Convert.ToInt32(o);
		if (typeof(T) == typeof(long))
			return (T)(object)Convert.ToInt64(o);
		if (typeof(T) == typeof(short))
			return (T)(object)Convert.ToInt16(o);
		if (typeof(T) == typeof(byte))
			return (T)(object)Convert.ToByte(o);
		if (typeof(T) == typeof(bool))
			return (T)(object)Convert.ToBoolean(o);
		if (typeof(T) == typeof(float))
			return (T)(object)Convert.ToSingle(o);
		if (typeof(T) == typeof(double))
			return (T)(object)Convert.ToDouble(o);
		if (o is null && (!typeof(T).IsValueType || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))))
			return default!;

		var stringWriter = new StringWriter();
		JSONSettings.serializer.Serialize(new JsonTextWriter(stringWriter), o);
		if (JSONSettings.serializer.Deserialize<T>(new JsonTextReader(new StringReader(stringWriter.ToString()))) is { } deserialized)
			return deserialized;

		throw new ArgumentException($"Cannot convert {o} to extension data type {typeof(T)}", nameof(T));
	}

	public T GetExtensionData<T>(IManifest manifest, object o, string key)
	{
		RegisterCloneListenerIfNeeded();
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
		RegisterCloneListenerIfNeeded();
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
		RegisterCloneListenerIfNeeded();
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
		RegisterCloneListenerIfNeeded();
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
		RegisterCloneListenerIfNeeded();
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

	internal void RegisterCloneListenerIfNeeded()
	{
		if (IsRegisteredCloneListener)
			return;

		var nickelStaticType = typeof(Nickel.Mod).Assembly.GetType("Nickel.NickelStatic");
		if (nickelStaticType is null)
			return; // outdated Nickel

		var cloneEngineField = AccessTools.DeclaredField(nickelStaticType, "CloneEngine");

		var cloneEngineLazy = (Lazy<DefaultCloneEngine>)cloneEngineField.GetValue(null)!;
		cloneEngineLazy.Value.RegisterCloneListener(this);
		IsRegisteredCloneListener = true;
	}

	public void OnClone<T>(ICloneEngine engine, T source, T destination) where T : class
	{
		if (source.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(source));
		if (destination.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(destination));

		if (!ExtensionDataStorage.TryGetValue(source, out var allSourceObjectData))
			return;

		if (!ExtensionDataStorage.TryGetValue(destination, out var allTargetObjectData))
		{
			allTargetObjectData = [];
			ExtensionDataStorage.AddOrUpdate(destination, allTargetObjectData);
		}

		foreach (var (modUniqueName, sourceModObjectData) in allSourceObjectData)
		{
			if (!allTargetObjectData.TryGetValue(modUniqueName, out var targetModObjectData))
			{
				targetModObjectData = [];
				allTargetObjectData[modUniqueName] = targetModObjectData;
			}

			foreach (var (key, value) in sourceModObjectData)
				targetModObjectData[key] = DeepCopy(value);
		}

		static object? DeepCopy(object? o)
		{
			if (o is null)
				return null;

			var type = o.GetType();
			if (type.IsValueType)
				return o;

			var stringWriter = new StringWriter();
			JSONSettings.serializer.Serialize(new JsonTextWriter(stringWriter), o);
			if (JSONSettings.serializer.Deserialize(new JsonTextReader(new StringReader(stringWriter.ToString()))) is { } deserialized)
				return deserialized;

			throw new ArgumentException($"Cannot deep copy {o}", nameof(o));
		}
	}
}