using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CustomRunOptions;

internal interface IRegisterable
{
	static virtual void Register(IPluginPackage<IModManifest> package, IModHelper helper) { }
}