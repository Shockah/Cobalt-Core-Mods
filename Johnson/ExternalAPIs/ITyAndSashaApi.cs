using Nickel;

namespace Shockah.Johnson;

public interface ITyAndSashaApi
{
	Deck TyDeck { get; }

	Status XFactorStatus { get; }

	ICardTraitEntry WildTrait { get; }
}