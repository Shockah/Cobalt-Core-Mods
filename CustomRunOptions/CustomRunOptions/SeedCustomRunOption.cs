using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal sealed class SeedCustomRunOption : ICustomRunOption
{
	private static ISpriteEntry Icon = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/SeedIcon.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.StartRun)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_StartRun_Prefix))
		);
	}

	public IReadOnlyList<ICustomRunOption.INewRunOptionsElement> GetNewRunOptionsElements(G g, RunConfig config)
		=> config.GetSeed() is null ? [] : [new IconNewRunOptionsElement(Icon.Sprite)];

	public IModSettingsApi.IModSetting MakeCustomRunSettings(IPluginPackage<IModManifest> package, IModSettingsApi api, RunConfig config)
		=> new IconAffixModSetting
		{
			Setting = new PasteModSetting
			{
				Title = () => ModEntry.Instance.Localizations.Localize(["options", nameof(SeedCustomRunOption), "title"]),
				ValueGetter = () => config.GetSeed()?.ToString(),
				ValueSetter = value => config.SetSeed(uint.TryParse(value, out var seed) ? seed : null)
			},
			LeftIcon = new() { Icon = Icon.Sprite },
		};

	private static void NewRunOptions_StartRun_Prefix(G g, ref uint? seed)
	{
		if (g.state.runConfig.GetSeed() is not { } forcedSeed)
			return;
		seed = forcedSeed;
	}
}

file static class RunConfigExt
{
	public static uint? GetSeed(this RunConfig config)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<uint>(config, "Seed");
	
	public static void SetSeed(this RunConfig config, uint? seed)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(config, "Seed", seed);
}