using System;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Destiny;

internal sealed class PristineShield : IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static IStatusEntry PristineShieldStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		PristineShieldStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("PristineShield", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/PristineShield.png")).Sprite,
				color = new Color("FF6FEC"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "PristineShield", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "PristineShield", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Prefix))
		);

		var instance = new PristineShield();
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(instance);
	}

	private static bool Ship_DirectHullDamage_Prefix(Ship __instance, int amt)
	{
		if (amt <= 0)
			return true;
		if (__instance.Get(Status.perfectShield) > 0)
			return true;
		if (__instance.Get(PristineShieldStatus.Status) <= 0)
			return true;
		
		__instance.Add(PristineShieldStatus.Status, -1);
		return false;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != PristineShieldStatus.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;
		if (args.Amount == 0)
			return false;

		args.Amount = Math.Max(args.Amount - 1, 0);
		return false;
	}
}