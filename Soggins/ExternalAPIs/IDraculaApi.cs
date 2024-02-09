using System;
using System.Collections.Generic;

namespace Shockah.Soggins;

public interface IDraculaApi
{
	void RegisterBloodTapOptionProvider(Status status, Func<State, Combat, Status, List<CardAction>> provider);
}