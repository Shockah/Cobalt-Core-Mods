namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IStatusNextTurnApi StatusNextTurn { get; }

		public interface IStatusNextTurnApi
		{
			Status Shield { get; }
			Status TempShield { get; }
		}
	}
}
