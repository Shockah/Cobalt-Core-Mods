using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}