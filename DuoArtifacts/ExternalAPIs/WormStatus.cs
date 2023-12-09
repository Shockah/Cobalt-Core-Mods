using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	ExternalStatus WormStatus { get; }
	Tooltip GetWormStatusTooltip(int? value = null);
}