using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal interface ICustomRunOption : IRegisterable
{
	IReadOnlyList<Vec> RenderInNewRunOptions(G g, Vec centerLinePosition, RunConfig runConfig);
	IModSettingsApi.IModSetting MakeCustomRunSettings(IPluginPackage<IModManifest> package, IModSettingsApi api, RunConfig runConfig);
}