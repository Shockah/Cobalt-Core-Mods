using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ISpontaneousApi Spontaneous { get; }

		public interface ISpontaneousApi
		{
			ICardTraitEntry SpontaneousTriggeredTrait { get; }
			
			public interface ISpontaneousAction : ICardAction<CardAction>
			{
				CardAction Action { get; set; }

				ISpontaneousAction SetAction(CardAction value);
			}
			
			ISpontaneousAction? AsAction(CardAction action);
			ISpontaneousAction MakeAction(CardAction action);
		}
	}
}
