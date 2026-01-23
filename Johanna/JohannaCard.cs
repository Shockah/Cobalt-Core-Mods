namespace Shockah.Johanna;

internal abstract class JohannaCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			artOverlay = ModEntry.GetCardRarity(GetType()) switch
			{
				Rarity.rare => ModEntry.Instance.RareCardFrame.Sprite,
				Rarity.uncommon => ModEntry.Instance.UncommonCardFrame.Sprite,
				_ => null,
			}
		};
}