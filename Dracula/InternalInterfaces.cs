using Nickel;

namespace Shockah.Dracula;

internal interface IDraculaCard
{
	static abstract void Register(IModHelper helper);

	float TextScaling
		=> 1f;

	float ActionSpacingScaling
		=> 1f;
}

internal interface IDraculaArtifact
{
	static abstract void Register(IModHelper helper);
}