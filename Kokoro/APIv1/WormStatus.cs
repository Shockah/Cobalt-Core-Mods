using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	ExternalStatus WormStatus { get; }
	Status WormVanillaStatus { get; }
	Tooltip GetWormStatusTooltip(int? value = null);
}