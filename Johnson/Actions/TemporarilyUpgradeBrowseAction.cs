namespace Shockah.Johnson;

public sealed class TemporarilyUpgradeBrowseAction : CardAction
{
	public int Discount = 0;
	public int Strengthen = 0;

	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		var baseResult = base.BeginWithRoute(g, s, c);
		if (selectedCard is null)
			return baseResult;

		selectedCard.discount += Discount;
		selectedCard.AddStrengthen(Strengthen);
		selectedCard.SetTemporarilyUpgraded(true);
		return new InPlaceCardUpgrade
		{
			cardCopy = Mutil.DeepCopy(selectedCard)
		};
	}
}