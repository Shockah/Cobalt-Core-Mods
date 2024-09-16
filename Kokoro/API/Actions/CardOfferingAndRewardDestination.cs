namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null);
		CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null);
	}
}