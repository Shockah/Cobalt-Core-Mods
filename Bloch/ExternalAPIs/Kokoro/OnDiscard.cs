namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IOnDiscardApi OnDiscard { get; }

		public interface IOnDiscardApi
		{
			public interface IOnDiscardAction : ICardAction<CardAction>
			{
				CardAction Action { get; set; }

				IOnDiscardAction SetAction(CardAction value);
			}
			
			IOnDiscardAction? AsAction(CardAction action);
			IOnDiscardAction MakeAction(CardAction action);
		}
	}
}
