namespace Shockah.Johnson;

public sealed class TemporarilyUpgradeBrowseAction : CardAction
{
	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		var baseResult = base.BeginWithRoute(g, s, c);
		if (selectedCard is null)
			return baseResult;

		selectedCard.SetTemporarilyUpgraded(true);
		return new InPlaceCardUpgrade
		{
			cardCopy = Mutil.DeepCopy(selectedCard)
		};
	}
}