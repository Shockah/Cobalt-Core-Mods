using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CustomRunOptions;

internal sealed class NewRunOptionsButton : IRegisterable
{
	private static readonly UK CustomOptionsUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	private static ISpriteEntry BackgroundOverlaySprite = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BackgroundOverlaySprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/NewRunOptionsOverlaySprite.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.RenderWarning)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_RenderWarning_Prefix_First)), priority: Priority.First)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.DifficultyOptions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_DifficultyOptions_Prefix))
		);
	}

	private static void NewRunOptions_RenderWarning_Prefix_First()
		=> Draw.Sprite(BackgroundOverlaySprite.Sprite, 0, 0);

	private static void NewRunOptions_DifficultyOptions_Prefix(G g, RunConfig runConfig)
	{
		SharedArt.ButtonText(g, new Vec(301, 70), CustomOptionsUk, "CUSTOM");
	}
}