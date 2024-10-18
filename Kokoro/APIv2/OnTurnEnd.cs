namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IOnTurnEndApi OnTurnEnd { get; }

		public interface IOnTurnEndApi
		{
			public interface IOnTurnEndAction : ICardAction
			{
				CardAction Action { get; set; }

				IOnTurnEndAction SetAction(CardAction value)
				{
					this.Action = value;
					return this;
				}
			}
			
			IOnTurnEndAction? AsAction(CardAction action);
			IOnTurnEndAction MakeAction(CardAction action);
		}
	}
}
