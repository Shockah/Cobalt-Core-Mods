using Nanoray.PluginManager;
using Nickel;

namespace Shockah.UISuite;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}