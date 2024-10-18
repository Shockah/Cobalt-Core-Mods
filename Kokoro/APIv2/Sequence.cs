namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ISequenceApi Sequence { get; }

		public interface ISequenceApi
		{
			public interface ISequenceAction : ICardAction<CardAction>
			{
				int CardId { get; set; }
				int SequenceStep { get; set; }
				int SequenceLength { get; set; }
				CardAction Action { get; set; }

				ISequenceAction SetCardId(int value);
				ISequenceAction SetSequenceStep(int value);
				ISequenceAction SetSequenceLength(int value);
				ISequenceAction SetAction(CardAction value);
			}
			
			ISequenceAction? AsAction(CardAction action);
			ISequenceAction MakeAction(int cardId, int sequenceStep, int sequenceLength, CardAction action);
		}
	}
}
