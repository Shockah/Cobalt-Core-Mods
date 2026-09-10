using Nanoray.EnumByNameSourceGenerator;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}

[EnumByName(typeof(Spr))]
internal static partial class StableSpr;

// [EnumByName(typeof(UK))]
// internal static partial class StableUK;