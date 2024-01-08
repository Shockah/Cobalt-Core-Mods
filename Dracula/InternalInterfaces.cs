using Nickel;

namespace Shockah.Dracula;

internal interface IDraculaCard
{
	void Register(IModHelper helper);

	float ActionRenderingSpacing
		=> 1;
}