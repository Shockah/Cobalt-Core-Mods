using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Natasha;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}