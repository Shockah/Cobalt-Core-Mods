using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bloch;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}