namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		CardAction MakeTimesPlayedVariableHintAction(int cardId);
		IConditionalActionApi.IIntExpression MakeTimesPlayedCondition(int currentTimesPlayed);

		int GetTimesPlayed(Card card);
		void SetTimesPlayed(Card card, int value);
	}
}
