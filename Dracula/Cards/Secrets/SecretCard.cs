namespace Shockah.Dracula;

internal abstract class SecretCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			singleUse = upgrade != Upgrade.A,
			exhaust = upgrade == Upgrade.A
		};
}