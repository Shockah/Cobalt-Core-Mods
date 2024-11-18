namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IOxidationStatusApi OxidationStatus { get; }

		public interface IOxidationStatusApi
		{
			Status Status { get; }
			
			int GetOxidationStatusMaxValue(State state, Ship ship);
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public interface IHook : IKokoroV2ApiHook
			{
				int ModifyOxidationRequirement(State state, Ship ship, int value) => 0;
			}
		}
	}
}
