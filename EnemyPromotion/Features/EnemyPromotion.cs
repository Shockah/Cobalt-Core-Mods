using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.EnemyPromotion;

internal sealed class EnemyPromotion : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MapBase), nameof(MapBase.PullEnemyFromHat)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapBase_PullEnemyFromHat_Transpiler))
		);
	}

	internal static AI Promote<T>(IEnemyPromotionApi.IPromotedEnemyHandlerArgs<T> args) where T : AI
	{
		ModEntry.Instance.Helper.ModData.SetModData(args.Enemy, "Promoted", true);
		return args.Enemy;
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> MapBase_PullEnemyFromHat_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Call(nameof(MapBase.GetEnemyPools)),
					ILMatches.Stloc<MapBase.MapEnemyPool>(originalMethod).GetLocalIndex(out var poolsLocalIndex),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloc, poolsLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapBase_PullEnemyFromHat_Transpiler_ModifyPools))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void MapBase_PullEnemyFromHat_Transpiler_ModifyPools(State state, BattleType battleType, Vec position, MapBase.MapEnemyPool pools)
	{
		if (battleType != BattleType.Elite)
			return;

		foreach (var enemy in pools.normal.Concat(pools.easy))
		{
			if (ModEntry.Instance.Api.PromoteEnemy(state, position, enemy) is not { } promotedEnemy)
				continue;
			pools.elites.Add(promotedEnemy);
		}
	}
}