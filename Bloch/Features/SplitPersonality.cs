using HarmonyLib;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.Bloch;

internal sealed class SplitPersonalityManager : IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry SplitPersonalityStatus { get; private set; } = null!;

	public SplitPersonalityManager()
	{
		SplitPersonalityStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("SplitPersonality", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/SplitPersonality.png")).Sprite,
				color = new("F82E2E"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "SplitPersonality", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "SplitPersonality", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(GetType(), nameof(AAttack_Begin_Prefix)), priority: Priority.Last)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(GetType(), nameof(ASpawn_Begin_Prefix)), priority: Priority.Last)
		);

		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance, State s, Combat c, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		if (__instance.fromDroneX is not null)
			return;
		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IsSplitPersonality"))
			return;

		var ship = __instance.targetPlayer ? c.otherShip : s.ship;
		if (ship.Get(SplitPersonalityStatus.Status) <= 0)
			return;
		if (ship.GetPartTypeCount(PType.cannon) > 1 && !__instance.multiCannonVolley)
			return;
		if (__instance.GetFromX(s, c) is not { } fromX)
			return;

		var copy = Mutil.DeepCopy(__instance);
		copy.fromX = ship.parts.Count - fromX - 1;
		copy.statusPulse = SplitPersonalityStatus.Status;
		ModEntry.Instance.Helper.ModData.SetModData(copy, "IsSplitPersonality", true);
		c.QueueImmediate([
			new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				status = SplitPersonalityStatus.Status,
				statusAmount = -1
			},
			copy
		]);
	}

	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IsSplitPersonality"))
			return;

		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.Get(SplitPersonalityStatus.Status) <= 0)
			return;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;

		var copy = Mutil.DeepCopy(__instance);
		copy.fromX = ship.parts.Count - __instance.GetWorldX(s, c) + ship.x - 1;
		copy.statusPulse = SplitPersonalityStatus.Status;
		ModEntry.Instance.Helper.ModData.SetModData(copy, "IsSplitPersonality", true);
		c.QueueImmediate([
			new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				status = SplitPersonalityStatus.Status,
				statusAmount = -1
			},
			copy
		]);
	}

	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == SplitPersonalityStatus.Status)
			return [.. args.Tooltips, new TTGlossary("action.spawn")];
		return args.Tooltips;
	}
}
