namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common)]
public abstract class ApologyCard : Card, IFrogproofCard
{
	public string? ApologyFlavorText;

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			temporary = true,
			exhaust = true,
			art = StableSpr.cards_colorless
		};

	public virtual double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> 1;
}
