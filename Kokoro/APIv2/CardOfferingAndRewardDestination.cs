namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ICardOfferingAndRewardDestinationApi CardOfferingAndRewardDestination { get; }

		public interface ICardOfferingAndRewardDestinationApi
		{
			public interface ICardOfferingAction : ICardAction<ACardOffering>
			{
				CardDestination? Destination { get; set; }
				bool? InsertRandomly { get; set; }

				ICardOfferingAction SetDestination(CardDestination? value);
				ICardOfferingAction SetInsertRandomly(bool? value);
			}
			
			public interface ICardReward : IRoute<CardReward>
			{
				CardDestination? Destination { get; set; }
				bool? InsertRandomly { get; set; }

				ICardReward SetDestination(CardDestination? value);
				ICardReward SetInsertRandomly(bool? value);
			}
			
			ICardOfferingAction? AsCardOffering(ACardOffering action);
			ICardOfferingAction MakeCardOffering(ACardOffering action);
			
			ICardReward? AsCardReward(CardReward route);
			ICardReward MakeCardReward(CardReward route);
		}
	}
}
