using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CustomRunOptions;

internal sealed class StartRunDetector : IRegisterable
{
	public static bool StartingNormalRun { get; private set; }
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.StartRun)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_StartRun_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_StartRun_Finalizer))
		);
	}

	private static void NewRunOptions_StartRun_Prefix()
		=> StartingNormalRun = true;

	private static void NewRunOptions_StartRun_Finalizer()
		=> StartingNormalRun = false;
}