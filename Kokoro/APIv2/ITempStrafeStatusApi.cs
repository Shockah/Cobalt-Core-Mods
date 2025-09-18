namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ITempStrafeStatusApi"/>
		ITempStrafeStatusApi TempStrafeStatus { get; }

		/// <summary>
		/// Allows accessing the Temp Strafe status, which is a temporary version of <see cref="Status.strafe"/>.
		/// </summary>
		public interface ITempStrafeStatusApi
		{
			/// <summary>
			/// Fire for N damage immediately after every move you make. Goes away at start of next turn.
			/// </summary>
			Status Status { get; }
		}
	}
}