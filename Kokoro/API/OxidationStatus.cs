using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	ExternalStatus OxidationStatus { get; }
	Tooltip GetOxidationStatusTooltip(Ship ship, State state);
	int GetOxidationStatusMaxValue(Ship ship, State state);
	void RegisterOxidationStatusHook(IOxidationStatusHook hook, double priority);
	void UnregisterOxidationStatusHook(IOxidationStatusHook hook);
}

public interface IOxidationStatusHook
{
	int ModifyOxidationRequirement(Ship ship, State state, int value);
}