using daisyowl.text;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IAssetsApi Assets { get; }

		public interface IAssetsApi
		{
			Font PinchCompactFont { get; }
		}
	}
}
