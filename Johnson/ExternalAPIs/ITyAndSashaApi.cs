namespace Shockah.Johnson;

public interface ITyAndSashaApi
{
	bool IsWild(Card card, State s, Combat c);

	Deck TyDeck { get; }
}