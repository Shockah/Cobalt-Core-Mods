using Nickel;

namespace Shockah.MORE;

public sealed class ApiImplementation : IMoreApi
{
	public IStatusEntry ActionReactionStatus
		=> MORE.ActionReactionStatus.Instance.Entry;
	
	public IStatusEntry VolatileOverdriveStatus
		=> MORE.VolatileOverdriveStatus.Instance.Entry;

	public void RegisterAltruisticArtifact(string key)
		=> ModEntry.Instance.AltruisticArtifactKeys.Add(key);

	//#region Self-destruct
	//public IStatusEntry SelfDestructTimerStatus
	//	=> BombEnemy.SelfDestructTimerStatus.Instance.Entry;

	//public Intent MakeSelfDestructIntent(int flatDamage = 0, double percentCurrentDamage = 0, double percentMaxDamage = 0, bool preventDeath = false)
	//	=> new BombEnemy.SelfDestructIntent
	//	{
	//		FlatDamage = flatDamage,
	//		PercentCurrentDamage = percentCurrentDamage,
	//		PercentMaxDamage = percentMaxDamage,
	//		PreventDeath = preventDeath,
	//	};
	//#endregion
}
