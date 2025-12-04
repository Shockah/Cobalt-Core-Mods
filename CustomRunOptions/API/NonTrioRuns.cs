namespace Shockah.CustomRunOptions;

public partial interface ICustomRunOptionsApi
{
	void RegisterDuoDeck(Deck deck1, Deck deck2, StarterDeck starterDeck);
	void RegisterPartialDuoDeck(Deck deck, StarterDeck starterDeck);
	void RegisterUnmannedDeck(string shipKey, StarterDeck starterDeck);

	StarterDeck? GetDuoDeck(Deck deck1, Deck deck2);
	StarterDeck? GetPartialDuoDeck(Deck deck);
	StarterDeck? GetUnmannedDuoDeck(string shipKey);

	StarterDeck MakeDefaultPartialDuoDeck(Deck deck);
	StarterDeck MakeDefaultUnmannedDeck(string shipKey);
}