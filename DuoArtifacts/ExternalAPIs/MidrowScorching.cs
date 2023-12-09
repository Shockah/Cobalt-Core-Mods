namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	Tooltip GetScorchingTooltip(int? value = null);
	int GetScorchingStatus(Combat combat, StuffBase @object);
	void SetScorchingStatus(Combat combat, StuffBase @object, int value);
	void AddScorchingStatus(Combat combat, StuffBase @object, int value);
}