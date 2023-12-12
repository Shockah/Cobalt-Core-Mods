using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	T GetExtensionData<T>(object o, string key) where T : notnull;
	bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data) where T : notnull;
	bool ContainsExtensionData(object o, string key);
	void SetExtensionData<T>(object o, string key, T data) where T : notnull;
	void RemoveExtensionData(object o, string key);
}
