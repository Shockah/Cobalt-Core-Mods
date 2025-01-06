using System;

namespace Nickel.Essentials;

/// <summary>
/// Provides access to <c>Nickel.Essentials</c> APIs.
/// </summary>
public interface IEssentialsApi
{
	/// <summary>
	/// Checks whether the given type represents an EXE card type (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>).
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>Whether the given type represents an EXE card type.</returns>
	bool IsExeCardType(Type type);
}
