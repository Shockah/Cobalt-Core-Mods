namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ICardDestinationApi CardDestination { get; }

		public interface ICardDestinationApi
		{
			ICardOffering ModifyCardOffering(ACardOffering action);
			
			ICardReward ModifyCardReward(CardReward route);
			
			public interface ICardOffering : ICardAction<ACardOffering>
			{
				CardDestination? Destination { get; set; }
				
				bool? InsertRandomly { get; set; }

				ICardOffering SetDestination(CardDestination? value);
				
				ICardOffering SetInsertRandomly(bool? value);
			}
			
			public interface ICardReward : IRoute<CardReward>
			{
				CardDestination? Destination { get; set; }
				
				bool? InsertRandomly { get; set; }

				ICardReward SetDestination(CardDestination? value);
				
				ICardReward SetInsertRandomly(bool? value);
			}
		}
	}
}
