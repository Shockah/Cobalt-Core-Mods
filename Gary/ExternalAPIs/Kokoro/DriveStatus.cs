namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IDriveStatusApi"/>
		IDriveStatusApi DriveStatus { get; }

		/// <summary>
		/// Allows accessing custom <see cref="Status">statuses</see>, which are variations on the <see cref="Status.overdrive"/> and <see cref="Status.powerdrive"/> statuses.
		/// </summary>
		public interface IDriveStatusApi
		{
			/// <summary>
			/// Decreases all attacks by the amount of stacks. Decreases by 1 each turn.
			/// </summary>
			Status Underdrive { get; }
			
			/// <summary>
			/// Increases all attacks by the amount of stacks. Resets to 0 each turn.
			/// </summary>
			Status Pulsedrive { get; }
			
			/// <summary>
			/// Increases all attacks by 1. Decreases by 1 each turn.
			/// </summary>
			Status Minidrive { get; }
		}
	}
}