namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ICustomCardUpgradeApi CustomCardUpgrade { get; }
		
		public interface ICustomCardUpgradeApi
		{
			public interface IRoute : IRoute<CardUpgrade>
			{
				bool IsInPlace { get; set; }

				IRoute SetInPlace(bool value);
			}

			IRoute MakeCustom(CardUpgrade route);
		}
	}
}
