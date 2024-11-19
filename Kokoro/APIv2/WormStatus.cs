namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IWormStatusApi WormStatus { get; }

		public interface IWormStatusApi
		{
			Status Status { get; }
		}
	}
}
