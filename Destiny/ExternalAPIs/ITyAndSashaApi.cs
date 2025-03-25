using Nickel;

namespace TheJazMaster.TyAndSasha;

public interface ITyAndSashaApi
{
	Deck TyDeck { get; }

	ICardTraitEntry WildTrait { get; }
}