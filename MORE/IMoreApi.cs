using Nickel;

namespace Shockah.MORE;

public interface IMoreApi
{
	IStatusEntry ActionReactionStatus { get; }
	IStatusEntry VolatileOverdriveStatus { get; }

	void RegisterAltruisticArtifact(string key);

	//#region Self-destruct
	//IStatusEntry SelfDestructTimerStatus { get; }

	//Intent MakeSelfDestructIntent(int flatDamage = 0, double percentCurrentDamage = 0, double percentMaxDamage = 0, bool preventDeath = false);
	//#endregion
}
