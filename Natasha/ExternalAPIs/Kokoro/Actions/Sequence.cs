namespace Shockah.Natasha;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		CardAction MakeSequenceAction(int cardId, int sequenceStep, int sequenceLength, CardAction action);
	}
}
