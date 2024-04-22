using System;

namespace Shockah.Dracula;

public interface IEssentialsApi
{
	Type? GetExeCardTypeForDeck(Deck deck);
	bool IsExeCardType(Type type);
}