namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IStatusNextTurnApi"/>
		IStatusNextTurnApi StatusNextTurn { get; }

		/// <summary>
		/// Allows accessing custom <see cref="Status">statuses</see>, which grant their respective proper statuses on the next turn of combat.
		/// </summary>
		public interface IStatusNextTurnApi
		{
			/// <summary>
			/// Grants <see cref="Status.shield"/> next turn.
			/// </summary>
			Status Shield { get; }
			
			/// <summary>
			/// Grants <see cref="Status.tempShield"/> next turn.
			/// </summary>
			Status TempShield { get; }
			
			/// <summary>
			/// Grants <see cref="Status.overdrive"/> next turn.
			/// </summary>
			Status Overdrive { get; }
		}
	}
}
