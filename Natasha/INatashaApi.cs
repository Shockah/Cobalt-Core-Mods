using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Natasha;

public interface INatashaApi
{
	IDeckEntry NatashaDeck { get; }

	IStatusEntry ReprogrammedStatus { get; }
	IStatusEntry DeprogrammedStatus { get; }

	CardAction MakeOneLinerAction(List<CardAction> actions, int spacing = 3);

	void RegisterManInTheMiddleStaticObject(ManInTheMiddleStaticObjectEntry entry);

	public record ManInTheMiddleStaticObjectEntry(
		string UniqueName,
		Func<State, StuffBase> Factory,
		double InitialWeight = 1,
		Func<State, double, double>? WeightProvider = null
	);
}