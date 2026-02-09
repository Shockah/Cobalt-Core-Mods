using daisyowl.text;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IAssetsApi"/>
		IAssetsApi Assets { get; }

		/// <summary>
		/// Allows accessing custom assets provided by Kokoro.
		/// </summary>
		public interface IAssetsApi
		{
			/// <summary>
			/// A modified, more compact version of the base game font (called "Pinch").
			/// </summary>
			/// <seealso cref="DB.pinch"/>
			Font PinchCompactFont { get; }
		}
	}
}
