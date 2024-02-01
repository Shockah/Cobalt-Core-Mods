namespace Shockah.Johnson;

public sealed class DiscountBrowseAction : CardAction
{
	public required int Amount;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (selectedCard is null)
			return;
		selectedCard.discount += Amount;
	}
}