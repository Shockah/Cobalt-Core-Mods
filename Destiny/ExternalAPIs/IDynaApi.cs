using Nickel;

namespace Shockah.Dyna;

public interface IDynaApi
{
	IDeckEntry DynaDeck { get; }

	AAttack SetBlastwave(AAttack attack, int? damage, int range = 1, bool isStunwave = false);
}