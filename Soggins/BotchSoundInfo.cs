using Nickel;

namespace Shockah.Soggins;

internal record struct BotchSoundInfo(
	ISoundEntry Entry,
	double ActionDelay = 0.2
);