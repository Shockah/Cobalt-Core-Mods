using Nanoray.PluginManager;
using Nickel;

namespace Shockah.EventsGalore;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}