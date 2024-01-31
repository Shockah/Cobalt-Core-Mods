namespace Shockah.Johnson;

public sealed class SpecificUpgradeBrowseAction : CardAction
{
	public required Upgrade Upgrade;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (selectedCard is null)
			return;
		selectedCard.upgrade = Upgrade;
	}
}