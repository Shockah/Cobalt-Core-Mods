namespace Shockah.Johnson;

public sealed class StrengthenBrowseAction : CardAction
{
	public required int Amount;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (selectedCard is null)
			return;
		selectedCard.AddStrengthen(Amount);
	}
}