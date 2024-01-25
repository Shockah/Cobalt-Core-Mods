using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Dracula;

public sealed class ApiImplementation : IDraculaApi
{
	public IDeckEntry DraculaDeck
		=> ModEntry.Instance.DraculaDeck;

	public IDeckEntry BatmobileDeck
		=> ModEntry.Instance.BatmobileDeck;

	public IStatusEntry BleedingStatus
		=> ModEntry.Instance.BleedingStatus;

	public IStatusEntry BloodMirrorStatus
		=> ModEntry.Instance.BloodMirrorStatus;

	public IStatusEntry TransfusionStatus
		=> ModEntry.Instance.TransfusionStatus;

	public IStatusEntry TransfusingStatus
		=> ModEntry.Instance.TransfusingStatus;

	public void RegisterBloodTapOptionProvider(Status status, Func<State, Combat, Status, List<CardAction>> provider)
		=> ModEntry.Instance.BloodTapManager.RegisterStatusOptionProvider(status, provider);

	public void RegisterBloodTapOptionProvider(IBloodTapOptionProvider provider, double priority = 0)
		=> ModEntry.Instance.BloodTapManager.RegisterStatusOptionProvider(provider, priority);
}
