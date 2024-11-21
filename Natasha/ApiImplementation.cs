using Nickel;
using System.Collections.Generic;

namespace Shockah.Natasha;

public sealed class ApiImplementation : INatashaApi
{
	public IDeckEntry NatashaDeck
		=> ModEntry.Instance.NatashaDeck;

	public IStatusEntry ReprogrammedStatus
		=> Reprogram.ReprogrammedStatus;

	public IStatusEntry DeprogrammedStatus
		=> Reprogram.DeprogrammedStatus;

	public CardAction MakeOneLinerAction(List<CardAction> actions, int spacing = 3)
		=> new OneLinerAction { Actions = actions, Spacing = spacing };

	public void RegisterManInTheMiddleStaticObject(INatashaApi.ManInTheMiddleStaticObjectEntry entry)
		=> ManInTheMiddleCard.RegisteredObjects[entry.UniqueName] = entry;
}
