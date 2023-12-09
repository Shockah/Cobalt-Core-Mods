using System;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	TimeSpan TotalGameTime { get; }
}