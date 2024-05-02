using Shockah.Shared;

namespace Shockah.Bloch;

public partial interface IKokoroApi : IProxyProvider
{
}

public interface IHookPriority
{
	double HookPriority { get; }
}