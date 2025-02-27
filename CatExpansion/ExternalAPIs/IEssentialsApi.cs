using System;

namespace Nickel.Essentials;

/// <summary>
/// Provides access to <c>Nickel.Essentials</c> APIs.
/// </summary>
public interface IEssentialsApi
{
	/// <summary>
	/// Returns the EXE card type (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>) for the given <see cref="Deck"/>.<br/>
	/// Takes into account EXE cards added by legacy mods, which are not available by reading <see cref="PlayableCharacterConfigurationV2.ExeCardType"/>.
	/// </summary>
	/// <param name="deck">The deck.</param>
	/// <returns>The EXE card type for the given <see cref="Deck"/>, or <c>null</c> if it does not have one assigned.</returns>
	Type? GetExeCardTypeForDeck(Deck deck);
	
	/// <summary>
	/// Checks whether the given type represents an EXE card type (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>).
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>Whether the given type represents an EXE card type.</returns>
	bool IsExeCardType(Type type);
}