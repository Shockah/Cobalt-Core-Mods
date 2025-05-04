using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal interface ICustomRunOption : IRegisterable
{
	IReadOnlyList<INewRunOptionsElement> GetNewRunOptionsElements(G g, RunConfig runConfig);
	IModSettingsApi.IModSetting MakeCustomRunSettings(IPluginPackage<IModManifest> package, IModSettingsApi api, RunConfig runConfig);

	public interface INewRunOptionsElement
	{
		Vec Size { get; }
		void Render(G g, Vec position);
	}
}