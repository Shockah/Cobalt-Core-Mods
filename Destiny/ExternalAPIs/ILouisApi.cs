using Nickel;

namespace TheJazMaster.Louis;

public interface ILouisApi
{
	int GemHandCount(State s, Combat c);

	Deck LouisDeck { get; }
	ICardTraitEntry GemTrait { get; }
}
