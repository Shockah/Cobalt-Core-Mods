using System;

namespace Shockah.Dyna;

public interface IMoreDifficultiesApi
{
	void RegisterAltStarters(Deck deck, StarterDeck starterDeck);

	Type BasicOffencesCardType { get; }
}