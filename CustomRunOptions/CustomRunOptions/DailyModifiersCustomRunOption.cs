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

	public IReadOnlyList<ICustomRunOption.INewRunOptionsElement> GetNewRunOptionsElements(G g, RunConfig runConfig)
		=> [
			// new ArtifactNewRunOptionsElement(new DailyBossBinaryStar()),
			// new ArtifactNewRunOptionsElement(new DailyCorrupted()),
			// new ArtifactNewRunOptionsElement(new DailyDraftPick()),
			// new ArtifactNewRunOptionsElement(new DailyEnemyShuffler()),
			// new ArtifactNewRunOptionsElement(new DailyJupiterToys()),
		];

	public IModSettingsApi.IModSetting MakeCustomRunSettings(IPluginPackage<IModManifest> package, IModSettingsApi api, RunConfig runConfig)
	{
		throw new System.NotImplementedException();
	}
}