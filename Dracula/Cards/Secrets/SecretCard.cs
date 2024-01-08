using Nickel;

namespace Shockah.Dracula;

internal abstract class SecretCard : Card, IDraculaCard
{
	public abstract void Register(IModHelper helper);

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			singleUse = upgrade != Upgrade.A,
			exhaust = upgrade == Upgrade.A
		};
}