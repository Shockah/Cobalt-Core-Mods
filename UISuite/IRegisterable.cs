using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal interface IRegisterable
{
	static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
	static virtual IModSettingsApi.IModSetting? MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api) => null;
	static virtual void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings) { }
}