using System;

namespace TheJazMaster.MoreDifficulties;

public interface IMoreDifficultiesApi
{
	StarterDeck? GetAltStarters(Deck deck);
	
	int Difficulty1 { get; }
	
	Type BasicOffencesCardType { get; }
	Type BasicDefencesCardType { get; }
	Type BasicManeuversCardType { get; }
	Type BasicBroadcastCardType { get; }

	bool IsLocked(State state, Deck deck);
}