using Nickel;

namespace Shockah.Dracula;

internal interface IDraculaCard
{
	static abstract void Register(IModHelper helper);

	float ActionRenderingSpacing
		=> 1;
}