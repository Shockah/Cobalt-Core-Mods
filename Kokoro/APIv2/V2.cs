using Shockah.Shared;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IV2 V2 { get; }
	
	public partial interface IV2 : IProxyProvider
	{
		public interface ICardAction
		{
			CardAction AsCardAction { get; }
		}
	}
}