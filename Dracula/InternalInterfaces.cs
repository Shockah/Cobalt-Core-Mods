using Nickel;

namespace Shockah.Dracula;

internal interface IDraculaCard
{
	static abstract void Register(IModHelper helper);
}

internal interface IDraculaArtifact
{
	static abstract void Register(IModHelper helper);
}