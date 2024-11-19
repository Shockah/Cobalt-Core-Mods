namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IRedrawStatusApi RedrawStatus { get; }

		public interface IRedrawStatusApi
		{
			Status Status { get; }
			
			IHook StandardRedrawStatusPaymentHook { get; }
			IHook StandardRedrawStatusActionHook { get; }
			
			bool IsRedrawPossible(State state, Combat combat, Card card);
			bool DoRedraw(State state, Combat combat, Card card);
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public interface IHook : IKokoroV2ApiHook
			{
				bool? CanRedraw(ICanRedrawArgs args) => null;
				bool PayForRedraw(IPayForRedrawArgs args) => false;
				bool DoRedraw(IDoRedrawArgs args) => false;
				void AfterRedraw(IAfterRedrawArgs args) { }
				
				public interface ICanRedrawArgs
				{
					State State { get; }
					Combat Combat { get; }
					Card Card { get; }
				}
				
				public interface IPayForRedrawArgs
				{
					State State { get; }
					Combat Combat { get; }
					Card Card { get; }
					IHook PossibilityHook { get; }
				}
				
				public interface IDoRedrawArgs
				{
					State State { get; }
					Combat Combat { get; }
					Card Card { get; }
					IHook PossibilityHook { get; }
					IHook PaymentHook { get; }
				}
				
				public interface IAfterRedrawArgs
				{
					State State { get; }
					Combat Combat { get; }
					Card Card { get; }
					IHook PossibilityHook { get; }
					IHook PaymentHook { get; }
					IHook ActionHook { get; }
				}
			}
		}
	}
}
