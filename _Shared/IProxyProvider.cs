using System.Diagnostics.CodeAnalysis;

namespace Shockah.Shared;

public interface IProxyProvider
{
	bool TryProxy<T>(object @object, [MaybeNullWhen(false)] out T proxy) where T : class;
}