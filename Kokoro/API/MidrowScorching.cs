namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	Tooltip GetScorchingTooltip(int? value = null);
	int GetScorchingStatus(Combat combat, StuffBase @object);
	void SetScorchingStatus(Combat combat, StuffBase @object, int value);
	void AddScorchingStatus(Combat combat, StuffBase @object, int value);
	void RegisterMidrowScorchingHook(IMidrowScorchingHook hook, double priority);
	void UnregisterMidrowScorchingHook(IMidrowScorchingHook hook);
}

public interface IMidrowScorchingHook
{
	void OnScorchingChange(Combat combat, StuffBase @object, int oldValue, int newValue);
}