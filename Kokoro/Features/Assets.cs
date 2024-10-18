using daisyowl.text;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IAssetsApi Assets { get; } = new AssetsApi();
		
		public sealed class AssetsApi : IKokoroApi.IV2.IAssetsApi
		{
			public Font PinchCompactFont
				=> ModEntry.Instance.Content.PinchCompactFont;
		}
	}
}