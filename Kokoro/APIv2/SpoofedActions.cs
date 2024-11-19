namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ISpoofedActionsApi SpoofedActions { get; }

		public interface ISpoofedActionsApi
		{
			public interface ISpoofedAction : ICardAction<CardAction>
			{
				CardAction RenderAction { get; set; }
				CardAction RealAction { get; set; }

				ISpoofedAction SetRenderAction(CardAction value);
				ISpoofedAction SetRealAction(CardAction value);
			}
			
			ISpoofedAction? AsAction(CardAction action);
			ISpoofedAction MakeAction(CardAction renderAction, CardAction realAction);
		}
	}
}
