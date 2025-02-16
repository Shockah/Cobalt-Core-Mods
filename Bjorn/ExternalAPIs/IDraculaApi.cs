using System;
using System.Collections.Generic;

namespace Shockah.Dracula;

public interface IDraculaApi
{
	void RegisterBloodTapOptionProvider(Status status, Func<State, Combat, Status, List<CardAction>> provider);
}