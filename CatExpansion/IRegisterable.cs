using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CatExpansion;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}