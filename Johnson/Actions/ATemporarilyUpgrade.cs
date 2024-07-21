namespace Shockah.Johnson;

public sealed class ATemporarilyUpgrade : CardAction
{
	public required int CardId;

	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		var baseResult = base.BeginWithRoute(g, s, c);
		if (s.FindCard(CardId) is not { } card || card.upgrade != Upgrade.None)
		{
			timer = 0;
			return baseResult;
		}

		card.SetTemporarilyUpgraded(true);
		return new InPlaceCardUpgrade
		{
			cardCopy = Mutil.DeepCopy(card)
		};
	}
}