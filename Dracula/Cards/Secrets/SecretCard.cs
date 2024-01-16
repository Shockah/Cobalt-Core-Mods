namespace Shockah.Dracula;

internal abstract class SecretCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 1 : 0,
			singleUse = upgrade == Upgrade.None,
			exhaust = upgrade == Upgrade.A
		};
}