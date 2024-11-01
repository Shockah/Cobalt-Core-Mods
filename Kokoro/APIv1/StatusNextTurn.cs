using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	ExternalStatus TempShieldNextTurnStatus { get; }
	Status TempShieldNextTurnVanillaStatus { get; }

	ExternalStatus ShieldNextTurnStatus { get; }
	Status ShieldNextTurnVanillaStatus { get; }
}