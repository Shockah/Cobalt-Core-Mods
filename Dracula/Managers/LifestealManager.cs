namespace Shockah.Dracula;

internal sealed class LifestealManager : IStatusRenderHook
{
	public LifestealManager()
	{
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
	}

	public bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount)
	{
		if (status != ModEntry.Instance.LifestealStatus.Status)
			return null;
		return false;
	}
}
