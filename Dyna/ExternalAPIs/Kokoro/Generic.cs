using Shockah.Shared;
using System;

namespace Shockah.Dyna;

public partial interface IKokoroApi : IProxyProvider
{
	TimeSpan TotalGameTime { get; }
}