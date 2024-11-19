namespace Shockah.Kokoro;

// TODO: V2

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		ICustomCardUpgrade MakeCustomCardUpgrade(CardUpgrade route);

		public interface ICustomCardUpgrade
		{
			CardUpgrade AsRoute { get; }
			
			bool IsInPlace { get; set; }

			ICustomCardUpgrade SetInPlace(bool value);
		}
	}
}