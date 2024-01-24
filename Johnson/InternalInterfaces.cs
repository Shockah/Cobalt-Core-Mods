using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Johnson;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}