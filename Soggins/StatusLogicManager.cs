using System.Collections.Generic;

namespace Shockah.Soggins;

internal sealed class StatusLogicManager : IStatusLogicHook
{
	private static ModEntry Instance => ModEntry.Instance;

	internal StatusLogicManager() : base()
	{
		Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
	}

	public IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(State state, Combat combat, Ship ship)
	{
		if (Instance.Api.GetSmug(ship) is not null)
			yield return (Status: (Status)Instance.SmugStatus.Id!.Value, Priority: 10);
	}

	public bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
		=> status == (Status)Instance.DoubleTimeStatus.Id!.Value ? false : null;
}
