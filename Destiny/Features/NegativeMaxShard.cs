using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Reflection;

namespace Shockah.Destiny;

internal sealed class NegativeMaxShard : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_CanBeNegative_Postfix))
		);
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == Status.maxShard)
			__result = true;
	}
}