using Nickel;

namespace Shockah.Johnson;

public interface ITyAndSashaApi
{
	Deck TyDeck { get; }

	ICardTraitEntry WildTrait { get; }
}