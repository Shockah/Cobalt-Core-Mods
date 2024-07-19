using Shockah.Shared;

namespace Shockah.Natasha;

public partial interface IKokoroApi : IProxyProvider
{
}

public interface IHookPriority
{
	double HookPriority { get; }
}