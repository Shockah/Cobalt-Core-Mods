using Nickel;

namespace Shockah.Johnson;

public interface ITyAndSashaApi
{
	Deck TyDeck { get; }

	Status XFactorStatus { get; }
	Status ExtremeMeasuresStatus { get; }

	ICardTraitEntry WildTrait { get; }
}