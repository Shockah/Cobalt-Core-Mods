using Nanoray.PluginManager;
using Nickel;

namespace Shockah.BetterCodex;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}