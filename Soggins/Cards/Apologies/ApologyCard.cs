namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common)]
public abstract class ApologyCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			temporary = true,
			exhaust = true
		};
}
