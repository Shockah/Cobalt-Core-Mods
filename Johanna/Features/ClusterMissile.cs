using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Johanna;

internal sealed class ClusterMissile : Missile, IRegisterable
{
	private record ClusterTypeDefinition(
		string SpritePath,
		string IconPath,
		MissileMetadata Meta
	)
	{
		public ISpriteEntry Sprite = null!;
		public ISpriteEntry Icon = null!;
	}
	
	private const MissileType NormalType = (MissileType)75834801;
	private const MissileType HEType = (MissileType)75834802;
	private const MissileType SeekerType = (MissileType)75834803;
	private const MissileType SeekerHEType = (MissileType)75834804;

	private static readonly Dictionary<MissileType, ClusterTypeDefinition> ClusterTypeDefinitions = new()
	{
		{ NormalType, new("assets/Midrow/NormalSprite.png", "assets/Midrow/NormalIcon.png", new() { exhaustColor = new("fff387"), baseDamage = 1 }) },
		{ HEType, new("assets/Midrow/HESprite.png", "assets/Midrow/HEIcon.png", new() { exhaustColor = new("ff5959"), baseDamage = 2 }) },
		{ SeekerType, new("assets/Midrow/SeekerSprite.png", "assets/Midrow/SeekerIcon.png", new() { exhaustColor = new("cc33ff"), baseDamage = 1, seeking = true }) },
		{ SeekerHEType, new("assets/Midrow/SeekerHESprite.png", "assets/Midrow/NormalIcon.png", new() { exhaustColor = new("e546ac"), baseDamage = 2, seeking = true }) }, // TODO: give it an icon
	};

	public int Count = 1;

	private readonly List<Vec> StationaryPositionCache = [];

	public ClusterMissile()
	{
		missileType = NormalType;
	}
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		foreach (var kvp in ClusterTypeDefinitions)
		{
			kvp.Value.Sprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile(kvp.Value.SpritePath));
			kvp.Value.Icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile(kvp.Value.IconPath));
			DB.missiles[kvp.Key] = kvp.Value.Sprite.Sprite;

			kvp.Value.Meta.key = $"{package.Manifest.UniqueName}::{kvp.Key}";
			kvp.Value.Meta.icon = kvp.Value.Icon.Sprite;
			missileData[kvp.Key] = kvp.Value.Meta;
		}
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMissileHit), nameof(AMissileHit.Update)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler))
		);
		
		ModEntry.Instance.KokoroApi.AttackLogic.RegisterHook(new AttackLogicHook());
	}

	public override List<Tooltip> GetTooltips()
		=> [
			.. base.GetTooltips(),
			// TODO: add real tooltips for clusters
		];

	public override void Render(G g, Vec v)
	{
		// render the hitting missile from the cluster
		if (isHitting)
			base.Render(g, v);
		
		// render the stationary missiles
		var missilesToRender = Count - (isHitting ? 1 : 0);
		if (missilesToRender <= 0)
			return;
		
		// figure out their positions
		StationaryPositionCache.Clear();
		for (var i = 0; i < missilesToRender; i++)
		{
			var fi = 1.0 * i / missilesToRender;
			var fx = (age + fi) / 1.5;
			var fy = (age + fi) / 3;
			var xx = Math.Cos(fx * Math.PI + Math.PI / 2) * 6;
			var yy = Math.Cos(fy * Math.PI) * 12;
			StationaryPositionCache.Add(new(xx, yy));
		}
		StationaryPositionCache.Sort((lhs, rhs) => lhs.y.CompareTo(rhs.y));
		
		// draw exhausts
		var mp = v + GetOffset(g, doRound: true);
		var exhaustOffset = targetPlayer ? Vec.Zero : new Vec(0, 21);
		var exhaustSprite = exhaustSprites.GetModulo((int)(g.state.time * 36 + x * 10));
		var exhaustSpriteOrigin = exhaustOffset + new Vec(1, 4);
		foreach (var position in StationaryPositionCache)
			Draw.Sprite(exhaustSprite, position.x + mp.x + exhaustSpriteOrigin.x, position.y + mp.y + exhaustSpriteOrigin.y + (targetPlayer ? 0 : 14), flipY: !targetPlayer, originRel: new(0, 1), color: missileData[missileType].exhaustColor);
		
		// draw missiles
		foreach (var position in StationaryPositionCache)
			DrawWithHilight(g, DB.missiles[missileType], mp + position);
	}

	public List<CardAction> GetActionsOnSpent(State state, Combat combat, int hitWorldX)
		=> [];
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AMissileHit_Update_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, ILMatches.Instruction(OpCodes.Ret))
				.CreateLabel(il, out var finalRetLabel)
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Isinst<Missile>(),
					ILMatches.Stloc<Missile>(originalMethod).GetLocalIndex(out var missileLocalIndex),
				])
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Call("RaycastGlobal"),
					ILMatches.Stloc<RaycastResult>(originalMethod).GetLocalIndex(out var rayLocalIndex),
				])
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldarg(3).ExtractLabels(out var labels),
					ILMatches.Ldfld("stuff"),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("worldX"),
					ILMatches.Call("Remove"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_2).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloc, missileLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, rayLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_HandleCluster))),
					new CodeInstruction(OpCodes.Brtrue, finalRetLabel)
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static bool AMissileHit_Update_Transpiler_HandleCluster(State state, Combat combat, Missile missile, RaycastResult ray)
	{
		if (missile is not ClusterMissile cluster)
			return false;

		cluster.Count--;
		if (cluster.Count <= 0)
			combat.QueueImmediate(cluster.GetActionsOnSpent(state, combat, ray.worldX));
		
		return cluster.Count > 0;
	}

	private sealed class AttackLogicHook : IKokoroApi.IV2.IAttackLogicApi.IHook
	{
		public bool? ModifyMidrowObjectVisuallyStoppingAttacks(IKokoroApi.IV2.IAttackLogicApi.IHook.IModifyMidrowObjectVisuallyStoppingAttacksArgs args)
		{
			if (args.Object is not ClusterMissile cluster)
				return null;
			if (cluster.targetPlayer)
				return true;
			return cluster.Count != 1;
		}
	}
}