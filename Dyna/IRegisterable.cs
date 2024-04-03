using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dyna;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}