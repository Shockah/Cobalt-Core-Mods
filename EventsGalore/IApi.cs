using Nickel;

namespace Shockah.EventsGalore;

public interface IApi
{
	IStatusEntry VolatileOverdriveStatus { get; }

	#region Self-destruct
	IStatusEntry SelfDestructTimerStatus { get; }

	Intent MakeSelfDestructIntent(int flatDamage = 0, double percentCurrentDamage = 0, double percentMaxDamage = 0, bool preventDeath = false);
	#endregion
}
