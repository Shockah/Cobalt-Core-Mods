using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal sealed class DailyModifiersCustomRunOption : ICustomRunOption
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		throw new System.NotImplementedException();
	}

	public IReadOnlyList<Vec> RenderInNewRunOptions(G g, Vec centerLinePosition, RunConfig runConfig)
	{
		throw new System.NotImplementedException();
	}

	public IModSettingsApi.IModSetting MakeCustomRunSettings(IPluginPackage<IModManifest> package, IModSettingsApi api, RunConfig runConfig)
	{
		throw new System.NotImplementedException();
	}
}