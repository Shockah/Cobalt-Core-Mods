namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ITimesPlayedApi TimesPlayed { get; }

		public interface ITimesPlayedApi
		{
			ITimesPlayedVariableHintAction? AsVariableHintAction(CardAction action);
			ITimesPlayedVariableHintAction MakeVariableHintAction(int cardId);
			
			ITimesPlayedConditionExpression? AsConditionExpression(IConditionalApi.IExpression expression);
			ITimesPlayedConditionExpression MakeConditionExpression(int currentTimesPlayed);
			
			int GetTimesPlayed(Card card);
			void SetTimesPlayed(Card card, int value);
			
			public interface ITimesPlayedVariableHintAction : ICardAction<AVariableHint>
			{
				int CardId { get; set; }

				ITimesPlayedVariableHintAction SetCardId(int value);
			}

			public interface ITimesPlayedConditionExpression : IConditionalApi.IIntExpression
			{
				int CurrentTimesPlayed { get; set; }
				
				ITimesPlayedConditionExpression SetCurrentTimesPlayed(int value);
			}
		}
	}
}
