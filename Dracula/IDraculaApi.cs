using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Dracula;

public interface IDraculaApi
{
	IDeckEntry DraculaDeck { get; }
	IDeckEntry SpellDeck { get; }
	IDeckEntry BatmobileDeck { get; }
	IStatusEntry BleedingStatus { get; }
	IStatusEntry BloodMirrorStatus { get; }
	IStatusEntry TransfusionStatus { get; }
	IStatusEntry TransfusingStatus { get; }

	void RegisterBloodTapOptionProvider(Status status, Func<State, Combat, Status, List<CardAction>> provider);
	void RegisterBloodTapOptionProvider(IBloodTapOptionProvider provider, double priority = 0);
}

public interface IBloodTapOptionProvider
{
	IEnumerable<Status> GetBloodTapApplicableStatuses(State state, Combat combat, IReadOnlySet<Status> allStatuses);
	IEnumerable<List<CardAction>> GetBloodTapOptionsActions(State state, Combat combat, IReadOnlySet<Status> allStatuses);
}