namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common)]
public abstract class ApologyCard : Card, IFrogproofCard
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			temporary = true,
			exhaust = true,
			art = StableSpr.cards_colorless
		};
}
