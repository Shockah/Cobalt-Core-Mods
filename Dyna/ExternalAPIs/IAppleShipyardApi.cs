using System;

namespace Shockah.Dyna;

public interface IAppleShipyardApi
{
	void RegisterActionLooksForPartType(Type actionType, PType partType);
}