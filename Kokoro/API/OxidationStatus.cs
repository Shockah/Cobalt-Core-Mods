using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	ExternalStatus OxidationStatus { get; }
	Status OxidationVanillaStatus { get; }
	Tooltip GetOxidationStatusTooltip(State state, Ship ship);
	int GetOxidationStatusMaxValue(State state, Ship ship);

	void RegisterOxidationStatusHook(IOxidationStatusHook hook, double priority);
	void UnregisterOxidationStatusHook(IOxidationStatusHook hook);
}

public interface IOxidationStatusHook
{
	int ModifyOxidationRequirement(State state, Ship ship, int value);
}