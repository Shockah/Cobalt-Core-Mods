namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IHiddenActionsApi HiddenActions { get; }

		public interface IHiddenActionsApi
		{
			public interface IHiddenAction : ICardAction<CardAction>
			{
				CardAction Action { get; set; }
				bool ShowTooltips { get; set; }

				IHiddenAction SetAction(CardAction value);
				IHiddenAction SetShowTooltips(bool value);
			}
			
			IHiddenAction? AsAction(CardAction action);
			IHiddenAction MakeAction(CardAction action);
		}
	}
}
