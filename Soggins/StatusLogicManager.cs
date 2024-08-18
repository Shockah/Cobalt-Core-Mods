namespace Shockah.Soggins;

internal sealed class StatusLogicManager : IStatusLogicHook
{
	private static ModEntry Instance => ModEntry.Instance;

	internal StatusLogicManager()
	{
		Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
	}

	public bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
		=> status == (Status)Instance.DoubleTimeStatus.Id!.Value || status == (Status)Instance.BotchesStatus.Id!.Value || status == (Status)Instance.SmugStatus.Id!.Value ? false : null;
}
