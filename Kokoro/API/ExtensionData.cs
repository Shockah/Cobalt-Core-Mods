using System;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	void RegisterTypeForExtensionData(Type type);
	T GetExtensionData<T>(object o, string key) where T : notnull;
	bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data) where T : notnull;
	T ObtainExtensionData<T>(object o, string key, Func<T> factory) where T : notnull;
	T ObtainExtensionData<T>(object o, string key) where T : notnull, new();
	bool ContainsExtensionData(object o, string key);
	void SetExtensionData<T>(object o, string key, T data) where T : notnull;
	void RemoveExtensionData(object o, string key);
}