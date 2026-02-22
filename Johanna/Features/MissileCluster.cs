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
using Shockah.Kokoro;

namespace Shockah.Johanna;

internal sealed class MissileCluster : Missile, IRegisterable
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

	public MissileCluster()
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
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix)), priority: Priority.VeryLow),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Finalizer)), priority: Priority.VeryHigh)
		);
		
		ModEntry.Instance.KokoroApi.AttackLogic.RegisterHook(new AttackLogicHook());
	}

	private string LocalizationKey
		=> missileType switch
		{
			NormalType => "Normal",
			HEType => "HE",
			SeekerType => "Seeker",
			SeekerHEType => "SeekerHE",
			_ => throw new ArgumentOutOfRangeException()
		};

	public override List<Tooltip> GetTooltips()
		=> base.GetTooltips().Select(tooltip =>
		{
			if (tooltip is not TTGlossary glossary || glossary.key != MKGlossary(missileData[missileType].key))
				return tooltip;
			return new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::Cluster::{missileData[missileType].key}")
			{
				Icon = GetIcon(),
				TitleColor = Colors.midrow,
				Title = ModEntry.Instance.Localizations.Localize(["midrow", "Cluster", LocalizationKey, "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["midrow", "Cluster", LocalizationKey, "description"], new { Count = Count }),
			};
		}).ToList();

	public override void Render(G g, Vec v)
	{
		// TODO: fix missing animation getting cut off
		
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

		var oldYAnimation = yAnimation;
		yAnimation = 1;
		try
		{
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
		finally
		{
			yAnimation = oldYAnimation;
		}
	}

	public bool IsSeeker
	{
		get => missileType is SeekerType or SeekerHEType;
		set
		{
			if (IsSeeker == value)
				return;
			missileType = missileType switch
			{
				NormalType => SeekerType,
				SeekerType => NormalType,
				HEType => SeekerHEType,
				SeekerHEType => HEType,
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}

	public bool IsHeavy
	{
		get => missileType is HEType or SeekerHEType;
		set
		{
			if (IsHeavy == value)
				return;
			missileType = missileType switch
			{
				NormalType => HEType,
				HEType => NormalType,
				SeekerType => SeekerHEType,
				SeekerHEType => SeekerType,
				_ => throw new ArgumentOutOfRangeException()
			};
		}
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
					new CodeInstruction(OpCodes.Ldarg_0),
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

	private static bool AMissileHit_Update_Transpiler_HandleCluster(State state, Combat combat, Missile missile, AMissileHit action, RaycastResult ray)
	{
		if (missile is not MissileCluster cluster)
			return false;

		cluster.Count--;
		if (cluster.Count <= 0)
			combat.QueueImmediate(cluster.GetActionsOnSpent(state, combat, ray.worldX));

		missile.isHitting = false;
		missile.yAnimation = 0;
		action.timer = 0;
		return cluster.Count > 0;
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.AnyLdloca.GetLocalIndex(out var capturesLocalIndex),
					ILMatches.Ldarg(3),
					ILMatches.Stfld("dontDraw"),
				])
				.Find(new ElementMatch<CodeInstruction>("call to the `IconAndOrNumber` local method", i => ILMatches.AnyCall.Matches(i) && ((MethodInfo)i.operand).Name.StartsWith("<RenderAction>g__IconAndOrNumber")))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloca, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldflda, AccessTools.DeclaredField(originalMethod.GetMethodBody()!.LocalVariables[capturesLocalIndex].LocalType, "w")),
					new CodeInstruction(OpCodes.Ldloca, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetMethodBody()!.LocalVariables[capturesLocalIndex].LocalType, "dontDraw")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_RenderClusterCount))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Card_RenderAction_Transpiler_RenderClusterCount(G g, CardAction action, ref int w, bool dontDraw)
	{
		if (action is not ASpawn launchAction)
			return;
		if (launchAction.thing is not MissileCluster missileCluster)
			return;

		w += 1;
		if (!dontDraw)
		{
			var box = g.Push(rect: new Rect(w));
			BigNumbers.Render(missileCluster.Count, box.rect.x, box.rect.y, action.disabled ? Colors.disabledText : Colors.textMain);
			g.Pop();
		}
		w += DB.IntStringCache(missileCluster.Count).Length * 6; // numberWidth
		w += 1;
	}

	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c, out MissileCluster? __state)
	{
		__state = null;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		
		var worldX = __instance.GetWorldX(s, c) + __instance.offset;
		var @object = c.stuff.GetValueOrDefault(worldX);
		if (@object is not MissileCluster cluster)
			return;
		
		__state = cluster;
		c.stuff.Remove(worldX);
	}

	private static void ASpawn_Begin_Finalizer(ASpawn __instance, State s, Combat c, in MissileCluster? __state)
	{
		if (__state is null)
			return;
		
		var worldX = __instance.GetWorldX(s, c) + __instance.offset;
		if (c.stuff.GetValueOrDefault(worldX) is not MissileCluster cluster)
			return;

		cluster.age = __state.age;
		cluster.Count += __state.Count;
		cluster.IsSeeker |= __state.IsSeeker;
		cluster.IsHeavy |= __state.IsHeavy;
	}

	private sealed class AttackLogicHook : IKokoroApi.IV2.IAttackLogicApi.IHook
	{
		public bool? ModifyMidrowObjectVisuallyStoppingAttacks(IKokoroApi.IV2.IAttackLogicApi.IHook.IModifyMidrowObjectVisuallyStoppingAttacksArgs args)
		{
			if (args.Object is not MissileCluster cluster)
				return null;
			if (cluster.targetPlayer)
				return true;
			return cluster.Count != 1;
		}
	}
}