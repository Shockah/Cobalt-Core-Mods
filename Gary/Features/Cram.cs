using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Gary;

internal sealed class Cram : IRegisterable, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry CramStatus { get; private set; } = null!;
	internal static IStatusEntry CramHarderStatus { get; private set; } = null!;
	
	internal static ISpriteEntry CrammedIcon { get; private set; } = null!;
	internal static ISpriteEntry CrammedLaunchIcon { get; private set; } = null!;

	private static StuffBase? ObjectBeingLaunchedInto;
	private static StuffBase? ObjectToPutLater;
	private static bool ObjectIsBeingCrammedInto;
	private static Guid? NestedJupiterShootBeginId;
	private static readonly List<(StuffBase RealObject, StuffBase? CrammedObject, int WorldX)?> ForceCrammedObjectStack = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		CramStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Cram", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Cram.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Cram", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Cram", "description"]).Localize
		});
		
		CramHarderStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("CramHarder", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/CramHarder.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "CramHarder", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "CramHarder", "description"]).Localize
		});

		CrammedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/Crammed.png"));
		CrammedLaunchIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/CrammedLaunch.png"));

		HandleLaunch();
		HandleDestroy();
		HandleTurnEnd();
		HandleRendering();
		HandleLifecycle();
		HandleMissiles();
		HandleJupiterDrones();
		HandleMedusaField();
		HandleCatch();
		HandleBubbleField();
		HandleRadioControl();

		var instance = new Cram();
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(instance);
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == CramStatus.Status)
			return [
				.. args.Tooltips,
				MakeCrammedMidrowAttributeTooltip(),
			];
		
		if (args.Status == CramHarderStatus.Status)
			return [
				.. args.Tooltips,
				.. StatusMeta.GetTooltips(CramStatus.Status, 1),
				MakeCrammedMidrowAttributeTooltip(),
			];

		return args.Tooltips;
	}

	internal static Tooltip MakeCrammedMidrowAttributeTooltip()
		=> new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::Crammed")
		{
			Icon = CrammedIcon.Sprite,
			TitleColor = Colors.midrow,
			Title = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Crammed", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Crammed", "description"]),
		};

	internal static Tooltip MakeCrammedLaunchTooltip()
		=> new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::CrammedLaunch")
		{
			Icon = CrammedLaunchIcon.Sprite,
			TitleColor = Colors.action,
			Title = ModEntry.Instance.Localizations.Localize(["action", "CrammedLaunch", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["action", "CrammedLaunch", "description"]),
		};

	internal static List<StuffBase>? GetCrammedObjects(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<List<StuffBase>>(@object, "CrammedObjects");

	internal static void SetCrammedObjects(StuffBase @object, List<StuffBase>? crammedObjects)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(@object, "CrammedObjects", crammedObjects);

	internal static void PushCrammedObject(Combat combat, int worldX, StuffBase pushed)
	{
		ref var @object = ref CollectionsMarshal.GetValueRefOrAddDefault(combat.stuff, worldX, out var objectExists);
		if (!objectExists)
		{
			Put();
			return;
		}

		List<StuffBase> crammedObjects = [
			.. GetCrammedObjects(@object!) ?? [],
			@object!,
			.. GetCrammedObjects(pushed) ?? [],
		];
		SetCrammedObjects(@object!, null);
		SetCrammedObjects(pushed, crammedObjects);

		Put();

		void Put()
		{
			if (ObjectBeingLaunchedInto is not null && !ObjectIsBeingCrammedInto)
				ObjectToPutLater = pushed;
			else
				combat.stuff[worldX] = pushed;

			UpdateCrammedObjectX(pushed, worldX, true);
		}
	}

	internal static bool PopCrammedObject(Combat combat, int worldX, bool removeLast)
	{
		if (!combat.stuff.Remove(worldX, out var @object))
			return false;
		if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
		{
			if (!removeLast)
				combat.stuff[worldX] = @object;
			return removeLast;
		}
		
		SetCrammedObjects(@object, null);
		@object = crammedObjects[^1];

		crammedObjects = crammedObjects.Count == 0 ? null : crammedObjects.Take(crammedObjects.Count - 1).ToList();
		SetCrammedObjects(@object, crammedObjects);
		combat.stuff[worldX] = @object;
		return true;
	}

	internal static bool RemoveCrammedObject(Combat combat, int worldX, StuffBase toRemove)
	{
		if (!combat.stuff.TryGetValue(worldX, out var @object))
			return false;

		if (@object == toRemove)
		{
			combat.stuff.Remove(worldX);
			
			if (GetCrammedObjects(@object) is { } crammedObjects && crammedObjects.Count != 0)
			{
				SetCrammedObjects(@object, null);
				@object = crammedObjects[^1];
				
				crammedObjects = crammedObjects.Count == 0 ? null : crammedObjects.Take(crammedObjects.Count - 1).ToList();
				SetCrammedObjects(@object, crammedObjects);
				combat.stuff[worldX] = @object;
			}
			
			return true;
		}

		if (GetCrammedObjects(@object) is { } crammedObjects2 && crammedObjects2.Count != 0)
		{
			SetCrammedObjects(toRemove, null);
			return crammedObjects2.Remove(toRemove);
		}
		
		return false;
	}

	internal static bool IsCrammed(ASpawn action)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(action, "IsCrammed");

	internal static void SetCrammed(ASpawn action, bool value = true)
		=> ModEntry.Instance.Helper.ModData.SetModData(action, "IsCrammed", value);
	
	private static Guid ObtainCrammedObjectId(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.ObtainModData(@object, "CrammedObjectId", Guid.NewGuid);

	private static void UpdateCrammedObjectX(StuffBase @object, int? maybeWorldX = null, bool updateXLerped = false)
	{
		var worldX = maybeWorldX ?? @object.x;
		@object.x = worldX;
		if (updateXLerped)
			@object.xLerped = worldX;

		if (GetCrammedObjects(@object) is { } crammedObjects)
		{
			foreach (var crammedObject in crammedObjects)
			{
				crammedObject.x = worldX;
				if (updateXLerped)
					crammedObject.xLerped = worldX;
			}
		}
	}

	internal static bool ApplyToAllCrammedObjects(Combat combat, Action<StuffBase> @delegate)
	{
		var hadAny = false;
		foreach (var @object in combat.stuff.Values)
		{
			if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
				continue;

			hadAny = true;
			foreach (var crammedObject in crammedObjects)
				@delegate(crammedObject);
		}
		return hadAny;
	}

	internal static bool AnyCrammedObject(Combat combat, Func<StuffBase, bool> @delegate)
	{
		foreach (var @object in combat.stuff.Values)
		{
			if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
				continue;
			if (crammedObjects.Any(@delegate))
				return true;
		}
		return false;
	}
	
	#region Launch
	private static void HandleLaunch()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler))
		);
	}
	
	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;

		var worldX = __instance.GetWorldX(s, c) + __instance.offset;
		if (!c.stuff.TryGetValue(worldX, out var existingThing))
			return;
		ObjectBeingLaunchedInto = existingThing;

		var crammedLaunch = IsCrammed(__instance);
		var cramAmount = ship.Get(CramStatus.Status);
		var cramHarderAmount = ship.Get(CramHarderStatus.Status);
		if (!crammedLaunch && cramAmount + cramHarderAmount <= 0)
			return;

		var stackSize = 1 + (GetCrammedObjects(existingThing)?.Count ?? 0);
		if (!crammedLaunch && stackSize > cramAmount + cramHarderAmount)
			return;

		if (!crammedLaunch)
		{
			var cramToRemove = stackSize - cramHarderAmount;
			ship.Add(CramStatus.Status, -cramToRemove);
		}

		ObjectIsBeingCrammedInto = true;
		c.stuff.Remove(worldX);
	}

	private static void ASpawn_Begin_Finalizer(ASpawn __instance, State s, Combat c)
	{
		if (ObjectBeingLaunchedInto is null)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		
		var worldX = __instance.GetWorldX(s, c) + __instance.offset;
		var existingObject = c.stuff.GetValueOrDefault(worldX);

		if (ObjectIsBeingCrammedInto)
		{
			c.stuff.Remove(worldX);
			PushCrammedObject(c, worldX, ObjectBeingLaunchedInto);
			if (existingObject is not null)
				PushCrammedObject(c, worldX, existingObject);
			ObjectIsBeingCrammedInto = false;
		}
		else if (ObjectToPutLater is not null)
		{
			c.stuff[worldX] = ObjectToPutLater;
			ObjectToPutLater = null;
		}
		
		ObjectBeingLaunchedInto = null;
	}

	private static void ASpawn_GetTooltips_Postfix(ASpawn __instance, ref List<Tooltip> __result)
	{
		if (!IsCrammed(__instance))
			return;

		List<Tooltip> tooltips = [
			MakeCrammedLaunchTooltip(),
			MakeCrammedMidrowAttributeTooltip(),
			.. StatusMeta.GetTooltips(CramStatus.Status, 1),
		];
		
		var index = __result.FindIndex(t => t is TTGlossary { key: "action.spawn" or "action.spawnOffsetLeft" or "action.spawnOffsetRight" });
		__result.InsertRange(index == -1 ? 0 : (index + 1), tooltips);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.AnyLdloca,
					ILMatches.Ldarg(3),
					ILMatches.Stfld("dontDraw").SelectElement(out var dontDrawField, i => (FieldInfo)i.operand!),
				])
				.Find([
					ILMatches.AnyLdloc.GetLocalIndex(out var capturesLocalIndex),
					ILMatches.Ldfld("w").SelectElement(out var wField, i => (FieldInfo)i.operand!),
				])
				.Find([
					ILMatches.Ldloc<ASpawn>(originalMethod).GetLocalIndex(out var actionLocalIndex),
					ILMatches.AnyLdloca,
					new ElementMatch<CodeInstruction>($"{{(any) call to local method named SpawnIcon}}", i => ILMatches.AnyCall.Matches(i) && (i.operand as MethodBase)?.Name.StartsWith("<RenderAction>g__SpawnIcon") == true),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, actionLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloc, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, dontDrawField.Value),
					new CodeInstruction(OpCodes.Ldloca, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldflda, wField.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_RenderCrammedLaunch))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Card_RenderAction_Transpiler_RenderCrammedLaunch(ASpawn action, G g, bool dontDraw, ref int width)
	{
		if (!IsCrammed(action))
			return;

		var box = g.Push(rect: new Rect(width));
		if (!dontDraw)
			Draw.Sprite(CrammedLaunchIcon.Sprite, box.rect.x, box.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		width += 9;
		g.Pop();
	}
	#endregion
	
	#region Destroy
	private static void HandleDestroy()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DestroyDroneAt)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Postfix))
		);
	}
	
	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, out StuffBase? __state)
	{
		__state = __instance.stuff.GetValueOrDefault(x);
		if (__state is { } @object)
			UpdateCrammedObjectX(@object, x);
	}

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, int x, in StuffBase? __state)
	{
		if (__state is null)
			return;

		if (__instance.stuff.Remove(x, out var existingThing))
		{
			PushCrammedObject(__instance, x, __state);
			PushCrammedObject(__instance, x, existingThing);
			return;
		}

		if (GetCrammedObjects(__state) is not { } crammedObjects || crammedObjects.Count == 0)
			return;
		
		SetCrammedObjects(__state, null);
		
		var newObject = crammedObjects[^1];
		crammedObjects = crammedObjects.Count == 0 ? null : crammedObjects.Take(crammedObjects.Count - 1).ToList();
		SetCrammedObjects(newObject, crammedObjects);
		
		PushCrammedObject(__instance, x, newObject);
	}
	#endregion
	
	#region Turn End
	private static void HandleTurnEnd()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneTurn), nameof(ADroneTurn.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_Begin_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> ADroneTurn_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Ldfld("stuff"),
					ILMatches.Ldloc<int>(originalMethod).GetLocalIndex(out var worldXLocalIndex),
					ILMatches.Call("get_Item"),
					ILMatches.Ldarg(2),
					ILMatches.Ldarg(3),
					ILMatches.Call("GetActions"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloc, worldXLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_Begin_Transpiler_ModifyActions))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static List<CardAction>? ADroneTurn_Begin_Transpiler_ModifyActions(List<CardAction>? actions, State state, Combat combat, int worldX)
	{
		if (!combat.stuff.TryGetValue(worldX, out var existingThing))
			return actions;
		if (GetCrammedObjects(existingThing) is not { } crammedObjects || crammedObjects.Count == 0)
			return actions;
		
		UpdateCrammedObjectX(existingThing, worldX);
		actions ??= [];
		actions.InsertRange(0, crammedObjects.SelectMany(crammedObject =>
		{
			if (crammedObject.GetActions(state, combat) is not { } actions)
				return [];

			var crammedObjectId = ObtainCrammedObjectId(crammedObject);
			return actions
				.Select(a =>
				{
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForceCrammedObjectId", crammedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForceCrammedObjectWorldX", worldX);
					return a;
				});
		}));
		return actions;
	}
	#endregion
	
	#region Rendering
	private static void HandleRendering()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDrones)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_RenderDrones_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex),
					ILMatches.Call("GetGetRect"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_OffsetMainObject))),
				])
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).ExtractLabels(out var labels),
					ILMatches.Ldarg(1),
					ILMatches.Ldloc<Box>(originalMethod).GetLocalIndex(out var boxLocalIndex),
					ILMatches.Ldflda("rect"),
					ILMatches.Call("get_xy"),
					ILMatches.Call("Render"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_RenderCrammedObjects))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static Rect Combat_RenderDrones_Transpiler_OffsetMainObject(Rect rect, StuffBase @object)
	{
		if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
			return rect;
		return new(rect.x, rect.y - crammedObjects.Count, rect.w, rect.h);
	}

	private static void Combat_RenderDrones_Transpiler_RenderCrammedObjects(G g, Box box, StuffBase @object)
	{
		if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
			return;

		for (var i = 0; i < crammedObjects.Count; i++)
			crammedObjects[i].Render(g, new Vec(box.rect.x + ((crammedObjects.Count - i) % 2 * 2 - 1) * 2, box.rect.y - crammedObjects.Count + (crammedObjects.Count - i) * 4));
		
		if (box.rect.x is > 60 and < 464 && box.IsHover())
		{
			var tooltipPos = box.rect.xy + new Vec(16, 24);
			g.tooltips.Add(tooltipPos, MakeCrammedMidrowAttributeTooltip());
			g.tooltips.Add(tooltipPos, ((IEnumerable<StuffBase>)crammedObjects).Reverse().SelectMany(crammedObject => crammedObject.GetTooltips()));
		}
	}
	#endregion
	
	#region Lifecycle
	private static void HandleLifecycle()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StuffBase_Update_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.ResetHilights)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_ResetHilights_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.BeginCardAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler))
		);
	}

	private static void PushForceCrammedObject(Combat combat, CardAction action)
	{
		(StuffBase RealObject, StuffBase? CrammedObject, int WorldX)? toPush = null;
		try
		{
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(action, "ForceCrammedObjectId", out var forceCrammedObjectId))
				return;
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<int>(action, "ForceCrammedObjectWorldX", out var forceCrammedObjectWorldX))
				return;
			if (!combat.stuff.TryGetValue(forceCrammedObjectWorldX, out var @object))
				return;

			if (ObtainCrammedObjectId(@object) == forceCrammedObjectId)
			{
				toPush = (@object, null, forceCrammedObjectWorldX);
				return;
			}
			
			if (GetCrammedObjects(@object) is not { } crammedObjects)
				return;
			if (crammedObjects.FirstOrDefault(crammedObject => ObtainCrammedObjectId(crammedObject) == forceCrammedObjectId) is not { } crammedObject)
				return;
			
			toPush = (@object, crammedObject, forceCrammedObjectWorldX);
			combat.stuff[forceCrammedObjectWorldX] = crammedObject;
		}
		finally
		{
			ForceCrammedObjectStack.Add(toPush);
		}
	}

	private static void PopForceCrammedObject(Combat combat)
	{
		if (ForceCrammedObjectStack.Count == 0)
			return;

		var nullableEntry = ForceCrammedObjectStack[^1];
		ForceCrammedObjectStack.RemoveAt(ForceCrammedObjectStack.Count - 1);

		if (nullableEntry?.RealObject is not { } realObject)
			return;
		var crammedObject = nullableEntry.Value.CrammedObject;
		var worldX = nullableEntry.Value.WorldX;
		
		var existingObject = combat.stuff.GetValueOrDefault(worldX);
		combat.stuff[worldX] = realObject;
		if (existingObject != crammedObject)
		{
			if (crammedObject is not null)
				RemoveCrammedObject(combat, worldX, crammedObject);
			if (existingObject is not null)
				PushCrammedObject(combat, worldX, existingObject);
		}
	}

	private static void StuffBase_Update_Postfix(StuffBase __instance, G g)
	{
		if (GetCrammedObjects(__instance) is { } crammedObjects)
			foreach (var crammedObject in crammedObjects)
				crammedObject.Update(g);
	}

	private static void Combat_ResetHilights_Postfix(Combat __instance)
	{
		ApplyToAllCrammedObjects(__instance, @object =>
		{
			if (@object.hilight > 0)
				@object.hilight--;
		});
	}

	private static void Combat_BeginCardAction_Prefix(Combat __instance, CardAction a)
		=> PushForceCrammedObject(__instance, a);

	private static void Combat_BeginCardAction_Finalizer(Combat __instance)
		=> PopForceCrammedObject(__instance);
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_DrainCardActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("currentCardAction"),
					ILMatches.Ldarg(1),
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldarg(0),
					ILMatches.Call("Update"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_PushForceCrammedObject))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_PopForceCrammedObject))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Combat_DrainCardActions_Transpiler_PushForceCrammedObject(Combat combat)
	{
		if (combat.currentCardAction is not { } action)
			return;
		PushForceCrammedObject(combat, action);
	}

	private static void Combat_DrainCardActions_Transpiler_PopForceCrammedObject(Combat combat)
		=> PopForceCrammedObject(combat);
	#endregion
	
	#region Missiles
	private static void HandleMissiles()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMissileHit), nameof(AMissileHit.Update)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AMissileHit_Update_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLabel(il, out var label)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_ShouldContinue))),
					new CodeInstruction(OpCodes.Brtrue, label),
					new CodeInstruction(OpCodes.Ret),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool AMissileHit_Update_Transpiler_ShouldContinue(AMissileHit action, G g, Combat combat)
	{
		if (!combat.stuff.TryGetValue(action.worldX, out var @object))
			return true;

		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(action, "ChecksForCrammedObjectId", out var checksForCrammedObjectId))
		{
			checksForCrammedObjectId = ObtainCrammedObjectId(@object);
			ModEntry.Instance.Helper.ModData.SetModData(action, "ChecksForCrammedObjectId", checksForCrammedObjectId);
		}

		if (checksForCrammedObjectId == ObtainCrammedObjectId(@object))
			return true;

		action.timer -= g.dt;
		return false;
	}
	#endregion
	
	#region Jupiter Drones
	private static void HandleJupiterDrones()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.DoWeHaveCannonsThough)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_DoWeHaveCannonsThough_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AJupiterShoot), nameof(AJupiterShoot.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AJupiterShoot_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AJupiterShoot_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(JupiterDroneHubV2), nameof(JupiterDroneHubV2.OnPlayerSpawnSomething)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(JupiterDroneHubV2_OnPlayerSpawnSomething_Prefix))
		);
	}
	
	private static void AAttack_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllCrammedObjects(combat, @object =>
		{
			if (@object is JupiterDrone)
				@object.hilight = 2;
		});
	}

	private static void AAttack_DoWeHaveCannonsThough_Postfix(State s, ref bool __result)
	{
		if (__result)
			return;
		if (s.route is not Combat combat)
			return;
		
		if (AnyCrammedObject(combat, @object => @object is JupiterDrone))
			__result = true;
	}

	private static void AJupiterShoot_Begin_Prefix(AJupiterShoot __instance, out Guid __state)
	{
		__state = NestedJupiterShootBeginId ?? Guid.NewGuid();
		ModEntry.Instance.Helper.ModData.SetModData(__instance.attackCopy, "IsFromAJupiterShoot", __state);
	}

	private static void AJupiterShoot_Begin_Postfix(AJupiterShoot __instance, G g, State s, Combat c, in Guid __state)
	{
		if (NestedJupiterShootBeginId is not null)
			return;
		
		List<(int WorldX, AAttack Attack, StuffBase TopObject, StuffBase? CrammedObject, int Depth)> attacks = [];
		for (var i = c.cardActions.Count - 1; i >= 0; i--)
		{
			if (c.cardActions[i] is not AAttack attack)
				continue;
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(attack, "IsFromAJupiterShoot", out var instanceId))
				continue;
			if (instanceId != __state)
				continue;
			if (attack.fromDroneX is null || !c.stuff.TryGetValue(attack.fromDroneX.Value, out var @object))
				continue;

			attacks.Add((attack.fromDroneX.Value, attack, @object, null, 0));
			c.cardActions.RemoveAt(i);
		}

		NestedJupiterShootBeginId = __state;
		var realStuff = c.stuff;
		
		try
		{
			List<(int WorldX, StuffBase TopObject, JupiterDrone JupiterDrone, int Depth)> crammedJupitedDrones = [];

			foreach (var kvp in c.stuff)
				if (GetCrammedObjects(kvp.Value) is { } crammedObjects)
					for (var i = 0; i < crammedObjects.Count; i++)
						if (crammedObjects[i] is JupiterDrone jupiterDrone)
							crammedJupitedDrones.Add((kvp.Key, kvp.Value, jupiterDrone, i + 1));
				
			c.stuff = [];

			while (crammedJupitedDrones.Count != 0)
			{
				var entry = crammedJupitedDrones[^1];
				crammedJupitedDrones.RemoveAt(crammedJupitedDrones.Count - 1);
				c.stuff[entry.WorldX] = entry.JupiterDrone;
				
				__instance.Begin(g, s, c);
				
				for (var i = c.cardActions.Count - 1; i >= 0; i--)
				{
					if (c.cardActions[i] is not AAttack attack)
						continue;
					if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(attack, "IsFromAJupiterShoot", out var instanceId))
						continue;
					if (instanceId != __state)
						continue;
					if (attack.fromDroneX is null || !c.stuff.ContainsKey(attack.fromDroneX.Value))
						continue;

					var crammedObjectId = ObtainCrammedObjectId(entry.JupiterDrone);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceCrammedObjectId", crammedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceCrammedObjectWorldX", entry.WorldX);
					
					attacks.Add((entry.WorldX, attack, entry.TopObject, entry.JupiterDrone, entry.Depth));
					c.cardActions.RemoveAt(i);
				}
			}
		}
		finally
		{
			c.stuff = realStuff;
			NestedJupiterShootBeginId = null;
		}
		
		c.QueueImmediate(
			attacks
				.OrderBy(e => e.WorldX)
				.ThenBy(e => e.Depth)
				.Select(e => e.Attack)
		);
	}

	private static bool JupiterDroneHubV2_OnPlayerSpawnSomething_Prefix(Combat combat, StuffBase thing)
	{
		if (thing is not JupiterDrone)
			return true;

		if (AnyCrammedObject(combat, @object => @object is JupiterDrone))
			return false;

		return true;
	}
	#endregion
	
	#region Medusa Field
	private static void HandleMedusaField()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMedusaField), nameof(AMedusaField.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMedusaField_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMedusaField_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMedusaField), nameof(AMedusaField.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMedusaField_GetTooltips_Postfix))
		);
	}
	
	private static void AMedusaField_Begin_Prefix(Combat c, out Dictionary<int, List<StuffBase>> __state)
	{
		__state = [];
		foreach (var kvp in c.stuff)
			if (GetCrammedObjects(kvp.Value) is { } crammedObjects)
				__state[kvp.Key] = crammedObjects;
	}

	private static void AMedusaField_Begin_Postfix(Combat c, in Dictionary<int, List<StuffBase>> __state)
	{
		foreach (var kvp in __state)
		{
			var crammedObjects = kvp.Value.Select(StuffBase (crammedObject) => new Geode
			{
				x = crammedObject.x,
				xLerped = crammedObject.xLerped,
				bubbleShield = crammedObject.bubbleShield,
				targetPlayer = crammedObject.targetPlayer,
				age = crammedObject.age,
			}).ToList();

			if (c.stuff.TryGetValue(kvp.Key, out var @object))
			{
				SetCrammedObjects(@object, crammedObjects);
			}
			else
			{
				foreach (var crammedObject in crammedObjects)
					PushCrammedObject(c, kvp.Key, crammedObject);
			}
		}
	}

	private static void AMedusaField_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllCrammedObjects(combat, @object =>
		{
			@object.hilight = 2;
		});
	}
	#endregion
	
	#region Catch
	private static void HandleCatch()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASlurpMidrowObject), nameof(ASlurpMidrowObject.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASlurpMidrowObject_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASlurpMidrowObject_Begin_Finalizer))
		);
	}

	private static void ASlurpMidrowObject_Begin_Prefix(Combat c, out Dictionary<int, List<StuffBase>> __state)
	{
		__state = [];
		foreach (var kvp in c.stuff)
		{
			if (GetCrammedObjects(kvp.Value) is not { } crammedObjects)
				continue;
			__state[kvp.Key] = crammedObjects;
			SetCrammedObjects(kvp.Value, null);
		}
	}

	private static void ASlurpMidrowObject_Begin_Finalizer(Combat c, in Dictionary<int, List<StuffBase>> __state)
	{
		foreach (var kvp in __state)
		{
			if (c.stuff.TryGetValue(kvp.Key, out var @object))
			{
				SetCrammedObjects(@object, kvp.Value);
			}
			else
			{
				foreach (var crammedObject in kvp.Value)
					PushCrammedObject(c, kvp.Key, crammedObject);
			}
		}
	}
	#endregion
	
	#region Bubble Field
	private static void HandleBubbleField()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ABubbleField), nameof(ABubbleField.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ABubbleField_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ABubbleField), nameof(ABubbleField.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ABubbleField_GetTooltips_Postfix))
		);
	}

	private static void ABubbleField_Begin_Postfix(Combat c)
	{
		ApplyToAllCrammedObjects(c, @object =>
		{
			@object.bubbleShield = true;
		});
	}

	private static void ABubbleField_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllCrammedObjects(combat, @object =>
		{
			@object.hilight = 2;
		});
	}
	#endregion
	
	#region Radio Control
	private static void HandleRadioControl()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneTurn), nameof(ADroneTurn.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_GetTooltips_Postfix))
		);
	}

	private static void ADroneTurn_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllCrammedObjects(combat, @object =>
		{
			if (@object.GetActions(s, combat) is not null)
				@object.hilight = 2;
		});
	}
	#endregion
}

internal static class CramExtensions
{
	public static ASpawn SetCrammed(this ASpawn action, bool value = true)
	{
		Cram.SetCrammed(action, value);
		return action;
	}
}