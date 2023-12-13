using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	ExternalStatus OxidationStatus { get; }
	Tooltip GetOxidationStatusTooltip(State state, Ship ship);
}