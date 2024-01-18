using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal interface IDraculaCard
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);

	float TextScaling
		=> 1f;

	float ActionSpacingScaling
		=> 1f;
}

internal interface IDraculaArtifact
{
	static abstract void Register(IModHelper helper);
}