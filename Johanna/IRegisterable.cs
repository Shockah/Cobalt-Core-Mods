using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Johanna;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}