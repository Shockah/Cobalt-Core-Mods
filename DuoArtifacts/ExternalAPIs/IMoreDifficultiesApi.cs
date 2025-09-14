namespace TheJazMaster.MoreDifficulties;

public interface IMoreDifficultiesApi
{
	bool HasAltStarters(Deck deck);
	StarterDeck? GetAltStarters(Deck deck);
	bool AreAltStartersEnabled(State state, Deck deck);
}