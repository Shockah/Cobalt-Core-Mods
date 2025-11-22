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

namespace Shockah.Gary;

internal sealed class Stack : IRegisterable, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry JengaStatus { get; private set; } = null!;

	private static ISpriteEntry StackedIcon = null!;
	private static ISpriteEntry WobblyIcon = null!;
	private static ISpriteEntry StackedLaunchIcon = null!;

	private static StuffBase? ObjectBeingLaunchedInto;
	private static StuffBase? ObjectToPutLater;
	private static bool ObjectIsBeingStackedInto;
	private static Guid? NestedJupiterShootBeginId;
	private static bool IsDuringDroneMove;
	private static bool IsDuringWobblyDestroy;
	private static readonly List<(StuffBase RealObject, StuffBase? StackedObject, int WorldX)?> ForceStackedObjectStack = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		JengaStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Jenga", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Jenga.png")).Sprite,
				color = new("FAE4BE"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Jenga", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Jenga", "description"]).Localize
		});

		StackedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/Stacked.png"));
		WobblyIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/Wobbly.png"));
		StackedLaunchIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/StackedLaunch.png"));

		HandleLaunch();
		HandleDestroy();
		HandleMove();
		HandleTurnEnd();
		HandleRendering();
		HandleLifecycle();
		HandleMissiles();
		HandleJupiterDrones();
		HandleMedusaField();
		HandleCatch();
		HandleBubbleField();
		HandleRadioControl();

		var instance = new Stack();
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(instance);
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> args.Status == JengaStatus.Status ? [
			.. args.Tooltips,
			MakeStackedMidrowAttributeTooltip(),
			MakeWobblyMidrowAttributeTooltip(),
		] : args.Tooltips;

	internal static Tooltip MakeStackedMidrowAttributeTooltip(int? count = null)
	{
		if (count is null)
			return new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::Stacked")
			{
				Icon = StackedIcon.Sprite,
				TitleColor = Colors.midrow,
				Title = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Stacked", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Stacked", "description"]),
			};
		else
			return new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::Stacked")
			{
				Icon = StackedIcon.Sprite,
				TitleColor = Colors.midrow,
				Title = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Stacked", "nameWithCount"]).Replace("{0}", $"<c=boldPink>{count.Value}</c>"),
				Description = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Stacked", "description"]),
				UppercaseTitle = false,
			};
	}

	internal static Tooltip MakeWobblyMidrowAttributeTooltip()
		=> new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::Wobbly")
		{
			Icon = WobblyIcon.Sprite,
			TitleColor = Colors.midrow,
			Title = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Wobbly", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["midrowAttribute", "Wobbly", "description"]),
		};

	internal static Tooltip MakeStackedLaunchTooltip()
		=> new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::StackedLaunch")
		{
			Icon = StackedLaunchIcon.Sprite,
			TitleColor = Colors.action,
			Title = ModEntry.Instance.Localizations.Localize(["action", "StackedLaunch", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["action", "StackedLaunch", "description"]),
		};

	#region State methods
	internal static List<StuffBase>? GetStackedObjects(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<List<StuffBase>>(@object, "StackedObjects");

	internal static void SetStackedObjects(StuffBase @object, List<StuffBase>? stackedObjects)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(@object, "StackedObjects", stackedObjects);
	
	internal static bool IsWobbly(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(@object, "IsWobbly");

	internal static void SetWobbly(StuffBase @object, bool value = true)
	{
		if (GetStackedObjects(@object) is { } stackedObjects)
			foreach (var stackedObject in stackedObjects)
				SetWobbly(stackedObject, false);
		
		if (value)
			ModEntry.Instance.Helper.ModData.SetModData(@object, "IsWobbly", true);
		else
			ModEntry.Instance.Helper.ModData.RemoveModData(@object, "IsWobbly");
	}

	private static void UpdateWobbly(StuffBase @object)
		=> SetWobbly(@object, IsWobbly(@object) || (GetStackedObjects(@object)?.Any(IsWobbly) ?? false));

	internal static void PushStackedObject(Combat combat, int worldX, StuffBase pushed)
	{
		if (!combat.stuff.TryGetValue(worldX, out var @object))
		{
			Put();
			return;
		}

		List<StuffBase> stackedObjects = [
			.. GetStackedObjects(@object) ?? [],
			@object,
			.. GetStackedObjects(pushed) ?? [],
		];
		SetStackedObjects(@object, null);
		SetStackedObjects(pushed, stackedObjects);

		Put();

		void Put()
		{
			if (ObjectBeingLaunchedInto is not null && !ObjectIsBeingStackedInto)
				ObjectToPutLater = pushed;
			else
				combat.stuff[worldX] = pushed;
			UpdateWobbly(pushed);

			UpdateStackedObjectX(pushed, worldX, true);
		}
	}

	internal static StuffBase? PopStackedObject(Combat combat, int worldX, bool removeLast)
	{
		if (!combat.stuff.Remove(worldX, out var @object))
			return null;
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
		{
			if (!removeLast)
				combat.stuff[worldX] = @object;
			UpdateWobbly(@object);
			return removeLast ? @object : null;
		}
		
		SetStackedObjects(@object, null);
		var newObject = stackedObjects[^1];

		stackedObjects = stackedObjects.Count == 0 ? null : stackedObjects.Take(stackedObjects.Count - 1).ToList();
		SetStackedObjects(newObject, stackedObjects);
		SetWobbly(newObject, IsWobbly(@object));
		combat.stuff[worldX] = newObject;
		UpdateWobbly(newObject);
		return @object;
	}

	internal static bool RemoveStackedObject(Combat combat, int worldX, StuffBase toRemove)
	{
		if (!combat.stuff.TryGetValue(worldX, out var @object))
			return false;

		if (@object == toRemove)
		{
			combat.stuff.Remove(worldX);
			
			if (GetStackedObjects(@object) is { } stackedObjects && stackedObjects.Count != 0)
			{
				SetStackedObjects(@object, null);
				var newObject = stackedObjects[^1];
				
				stackedObjects = stackedObjects.Count == 0 ? null : stackedObjects.Take(stackedObjects.Count - 1).ToList();
				SetStackedObjects(newObject, stackedObjects);
				SetWobbly(newObject, IsWobbly(@object));
				combat.stuff[worldX] = newObject;
				UpdateWobbly(newObject);
			}
			
			return true;
		}

		if (GetStackedObjects(@object) is { } stackedObjects2 && stackedObjects2.Count != 0)
		{
			SetStackedObjects(toRemove, null);
			return stackedObjects2.Remove(toRemove);
		}
		
		return false;
	}

	internal static bool IsStacked(ASpawn action)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(action, "IsStacked");

	internal static void SetStacked(ASpawn action, bool value = true)
		=> ModEntry.Instance.Helper.ModData.SetModData(action, "IsStacked", value);
	
	private static Guid ObtainStackedObjectId(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.ObtainModData(@object, "StackedObjectId", Guid.NewGuid);

	private static void UpdateStackedObjectX(StuffBase @object, int? maybeWorldX = null, bool updateXLerped = false)
	{
		var worldX = maybeWorldX ?? @object.x;
		@object.x = worldX;
		if (updateXLerped)
			@object.xLerped = worldX;

		if (GetStackedObjects(@object) is { } stackedObjects)
		{
			foreach (var stackedObject in stackedObjects)
			{
				stackedObject.x = worldX;
				if (updateXLerped)
					stackedObject.xLerped = worldX;
			}
		}
	}

	internal static bool ApplyToAllStackedObjects(Combat combat, Action<StuffBase> @delegate)
	{
		var hadAny = false;
		foreach (var @object in combat.stuff.Values)
		{
			if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
				continue;

			hadAny = true;
			foreach (var stackedObject in stackedObjects)
				@delegate(stackedObject);
		}
		return hadAny;
	}

	internal static bool AnyStackedObject(Combat combat, Func<StuffBase, bool> @delegate)
	{
		foreach (var @object in combat.stuff.Values)
		{
			if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
				continue;
			if (stackedObjects.Any(@delegate))
				return true;
		}
		return false;
	}
	#endregion
	
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

		var willStack = IsStacked(__instance);

		if (!willStack)
		{
			var stackSize = 1 + (GetStackedObjects(existingThing)?.Count ?? 0);
			var jengaAmount = ship.Get(JengaStatus.Status);

			if (jengaAmount > 0)
			{
				willStack = true;
				ship.Add(JengaStatus.Status, -Math.Min(jengaAmount, stackSize));
				if (stackSize > jengaAmount)
					SetWobbly(__instance.thing);
			}
		}

		if (!willStack)
			return;

		ObjectIsBeingStackedInto = true;
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

		if (ObjectIsBeingStackedInto)
		{
			c.stuff.Remove(worldX);
			PushStackedObject(c, worldX, ObjectBeingLaunchedInto);
			if (existingObject is not null)
				PushStackedObject(c, worldX, existingObject);
			ObjectIsBeingStackedInto = false;
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
		if (IsWobbly(__instance.thing))
		{
			List<Tooltip> tooltipsToInsert = [
				MakeStackedLaunchTooltip(),
				MakeStackedMidrowAttributeTooltip(),
				MakeWobblyMidrowAttributeTooltip(),
			];
		
			var index = __result.FindIndex(t => t is TTGlossary { key: "action.spawn" or "action.spawnOffsetLeft" or "action.spawnOffsetRight" });
			__result.InsertRange(index == -1 ? 0 : (index + 1), tooltipsToInsert);
		}
		else if (IsStacked(__instance))
		{
			List<Tooltip> tooltipsToInsert = [
				MakeStackedLaunchTooltip(),
				MakeStackedMidrowAttributeTooltip(),
			];
		
			var index = __result.FindIndex(t => t is TTGlossary { key: "action.spawn" or "action.spawnOffsetLeft" or "action.spawnOffsetRight" });
			__result.InsertRange(index == -1 ? 0 : (index + 1), tooltipsToInsert);
		}
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_RenderStackedLaunch))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Card_RenderAction_Transpiler_RenderStackedLaunch(ASpawn action, G g, bool dontDraw, ref int width)
	{
		const int leftPad = -2;

		var isWobbly = IsWobbly(action.thing);
		var isStacked = isWobbly || IsStacked(action);
		
		if (isStacked)
		{
			var stackedBox = g.Push(rect: new Rect(width + leftPad));
			if (!dontDraw)
				Draw.Sprite(StackedLaunchIcon.Sprite, stackedBox.rect.x, stackedBox.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
			width += 9 + leftPad;
			g.Pop();
			
			if (isWobbly)
			{
				var wobblyBox = g.Push(rect: new Rect(width + leftPad));
				if (!dontDraw)
					Draw.Sprite(WobblyIcon.Sprite, wobblyBox.rect.x, wobblyBox.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
				width += 8;
				g.Pop();
			}
		}
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
			UpdateStackedObjectX(@object, x);
	}

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, State s, int x, bool playerDidIt, in StuffBase? __state)
	{
		if (IsDuringWobblyDestroy)
			return;
		if (__state is null)
			return;

		if (__instance.stuff.Remove(x, out var existingThing))
		{
			PushStackedObject(__instance, x, __state);
			PushStackedObject(__instance, x, existingThing);
			return;
		}

		if (GetStackedObjects(__state) is not { } stackedObjects || stackedObjects.Count == 0)
			return;
		
		SetStackedObjects(__state, null);

		if (IsWobbly(__state))
		{
			IsDuringWobblyDestroy = true;
			try
			{
				var newObjects = new List<StuffBase>();
				
				while (stackedObjects.Count != 0)
				{
					var lastStackedObject = stackedObjects[^1];
					stackedObjects.RemoveAt(stackedObjects.Count - 1);
					
					__instance.stuff[x] = lastStackedObject;
					__instance.DestroyDroneAt(s, x, playerDidIt);
					
					if (__instance.stuff.Remove(x, out var existingThing2))
					{
						if (GetStackedObjects(existingThing2) is { } stackedObjects2)
							newObjects.AddRange(stackedObjects2);
						newObjects.Add(existingThing2);
					}
				}

				if (newObjects.Count != 0)
				{
					var newObject = newObjects[^1];
					stackedObjects = newObjects.Count > 1 ? newObjects.Take(newObjects.Count - 1).ToList() : null;
					SetStackedObjects(newObject, stackedObjects);
					__instance.stuff[x] = newObject;
				}
			}
			finally
			{
				IsDuringWobblyDestroy = false;
			}
		}
		else
		{
			var newObject = stackedObjects[^1];
			stackedObjects = stackedObjects.Count == 0 ? null : stackedObjects.Take(stackedObjects.Count - 1).ToList();
			SetStackedObjects(newObject, stackedObjects);
			SetWobbly(newObject, false);
			
			PushStackedObject(__instance, x, newObject);
		}
	}
	#endregion
	
	#region Move
	private static void HandleMove()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneMove), nameof(ADroneMove.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneMove), nameof(ADroneMove.DoMoveSingleDrone)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Transpiler))
		);
	}

	private static void ADroneMove_Begin_Prefix()
		=> IsDuringDroneMove = true;

	private static void ADroneMove_Begin_Finalizer()
		=> IsDuringDroneMove = false;

	private static void ADroneMove_DoMoveSingleDrone_Prefix(Combat c, int x, out StuffBase? __state)
	{
		if (IsDuringDroneMove)
		{
			__state = null;
			return;
		}

		var toMove = PopStackedObject(c, x, true);
		var leftover = c.stuff.GetValueOrDefault(x);
		__state = leftover;

		if (toMove is null)
			c.stuff.Remove(x);
		else
			c.stuff[x] = toMove;
	}

	private static void ADroneMove_DoMoveSingleDrone_Finalizer(Combat c, int x, in StuffBase? __state)
	{
		if (IsDuringDroneMove)
			return;
		if (__state is null)
			return;
		
		var potentialNewStuff = c.stuff.GetValueOrDefault(x);
		c.stuff.Remove(x);
		
		PushStackedObject(c, x, __state);
		if (potentialNewStuff is not null)
			PushStackedObject(c, x, potentialNewStuff);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> ADroneMove_DoMoveSingleDrone_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex),
					ILMatches.Call("Invincible"),
					ILMatches.Brfalse,
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Transpiler_ApplyFakeWobblyIfNeeded))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void ADroneMove_DoMoveSingleDrone_Transpiler_ApplyFakeWobblyIfNeeded(StuffBase @object, Combat combat)
	{
		if (!combat.stuff.TryGetValue(@object.x, out var existingObject))
			return;
		if (!@object.Invincible())
			return;
		
		SetWobbly(existingObject);
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
					ILMatches.Ldfld(nameof(Combat.stuff)),
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
		if (GetStackedObjects(existingThing) is not { } stackedObjects || stackedObjects.Count == 0)
			return actions;
		
		UpdateStackedObjectX(existingThing, worldX);
		actions ??= [];
		actions.InsertRange(0, stackedObjects.SelectMany(stackedObject =>
		{
			if (stackedObject.GetActions(state, combat) is not { } actions)
				return [];

			var stackedObjectId = ObtainStackedObjectId(stackedObject);
			return actions
				.Select(a =>
				{
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForceStackedObjectId", stackedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForceStackedObjectWorldX", worldX);
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
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrawIntentLinesForPart)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrawIntentLinesForPart_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_RenderDrones_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var oldRectLocal = il.DeclareLocal(typeof(Rect));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex),
					ILMatches.Call("GetGetRect"),
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_RenderStackedObjects))),
					
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Box), nameof(Box.rect))),
					new CodeInstruction(OpCodes.Stloc, oldRectLocal),
					
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_OffsetMainObject))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, oldRectLocal),
					new CodeInstruction(OpCodes.Stfld, AccessTools.DeclaredField(typeof(Box), nameof(Box.rect))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static double GetWobbleOffset(G g, int depth)
		=> Math.Sin((g.state.time + depth) * 1.5) * 2;

	private static void Combat_RenderDrones_Transpiler_RenderStackedObjects(G g, Box box, StuffBase @object)
	{
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
			return;
		var isWobbly = IsWobbly(@object);

		for (var i = 0; i < stackedObjects.Count; i++)
		{
			var offset = isWobbly ? GetWobbleOffset(g, i + 1) : 0;
			stackedObjects[i].Render(g, new Vec(box.rect.x + offset + ((stackedObjects.Count - i) % 2 * 2 - 1) * 2, box.rect.y - stackedObjects.Count + (stackedObjects.Count - i) * 4));
		}
		
		if (box.rect.x is > 60 and < 464 && box.IsHover())
		{
			var tooltipPos = box.rect.xy + new Vec(16, 24);
			g.tooltips.Add(tooltipPos, MakeStackedMidrowAttributeTooltip(stackedObjects.Count + 1));
			if (IsWobbly(@object))
				g.tooltips.Add(tooltipPos, MakeWobblyMidrowAttributeTooltip());
			g.tooltips.Add(tooltipPos, ((IEnumerable<StuffBase>)stackedObjects).Reverse().SelectMany(stackedObject => stackedObject.GetTooltips()));
		}
	}

	private static void Combat_RenderDrones_Transpiler_OffsetMainObject(G g, Box box, StuffBase @object)
	{
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
			return;
		var offset = IsWobbly(@object) ? GetWobbleOffset(g, 0) : 0;
		box.rect = new(box.rect.x + offset, box.rect.y - stackedObjects.Count, box.rect.w, box.rect.h);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_DrawIntentLinesForPart_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex).ExtractLabels(out var labels),
					ILMatches.Isinst<Missile>(),
					ILMatches.Brfalse.GetBranchTarget(out var renderDroneEndCapLabel),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrawIntentLinesForPart_Transpiler_IsStackBlocking))),
					new CodeInstruction(OpCodes.Brtrue, renderDroneEndCapLabel.Value),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool Combat_DrawIntentLinesForPart_Transpiler_IsStackBlocking(StuffBase @object, Ship shipSource)
	{
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
			return false;
		if (shipSource.isPlayerShip)
			return true;
		// TODO: Jack compat, if needed
		foreach (var stackedObject in stackedObjects)
			if (stackedObject is not Missile)
				return true;
		return false;
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

	private static void PushForceStackedObject(Combat combat, CardAction action)
	{
		(StuffBase RealObject, StuffBase? StackedObject, int WorldX)? toPush = null;
		try
		{
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(action, "ForceStackedObjectId", out var forceStackedObjectId))
				return;
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<int>(action, "ForceStackedObjectWorldX", out var forceStackedObjectWorldX))
				return;
			if (!combat.stuff.TryGetValue(forceStackedObjectWorldX, out var @object))
				return;

			if (ObtainStackedObjectId(@object) == forceStackedObjectId)
			{
				toPush = (@object, null, forceStackedObjectWorldX);
				return;
			}
			
			if (GetStackedObjects(@object) is not { } stackedObjects)
				return;
			if (stackedObjects.FirstOrDefault(stackedObject => ObtainStackedObjectId(stackedObject) == forceStackedObjectId) is not { } stackedObject)
				return;
			
			toPush = (@object, stackedObject, forceStackedObjectWorldX);
			combat.stuff[forceStackedObjectWorldX] = stackedObject;
		}
		finally
		{
			ForceStackedObjectStack.Add(toPush);
		}
	}

	private static void PopForceStackedObject(Combat combat)
	{
		if (ForceStackedObjectStack.Count == 0)
			return;

		var nullableEntry = ForceStackedObjectStack[^1];
		ForceStackedObjectStack.RemoveAt(ForceStackedObjectStack.Count - 1);

		if (nullableEntry?.RealObject is not { } realObject)
			return;
		var stackedObject = nullableEntry.Value.StackedObject;
		var worldX = nullableEntry.Value.WorldX;
		
		var existingObject = combat.stuff.GetValueOrDefault(worldX);
		combat.stuff[worldX] = realObject;
		if (existingObject != stackedObject)
		{
			if (stackedObject is not null)
				RemoveStackedObject(combat, worldX, stackedObject);
			if (existingObject is not null)
				PushStackedObject(combat, worldX, existingObject);
		}
	}

	private static void StuffBase_Update_Postfix(StuffBase __instance, G g)
	{
		if (GetStackedObjects(__instance) is { } stackedObjects)
			foreach (var stackedObject in stackedObjects)
				stackedObject.Update(g);
	}

	private static void Combat_ResetHilights_Postfix(Combat __instance)
	{
		ApplyToAllStackedObjects(__instance, @object =>
		{
			if (@object.hilight > 0)
				@object.hilight--;
		});
	}

	private static void Combat_BeginCardAction_Prefix(Combat __instance, CardAction a)
		=> PushForceStackedObject(__instance, a);

	private static void Combat_BeginCardAction_Finalizer(Combat __instance)
		=> PopForceStackedObject(__instance);
	
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_PushForceStackedObject))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_PopForceStackedObject))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Combat_DrainCardActions_Transpiler_PushForceStackedObject(Combat combat)
	{
		if (combat.currentCardAction is not { } action)
			return;
		PushForceStackedObject(combat, action);
	}

	private static void Combat_DrainCardActions_Transpiler_PopForceStackedObject(Combat combat)
		=> PopForceStackedObject(combat);
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
				.Find(ILMatches.Stloc<Missile>(originalMethod).GetLocalIndex(out var missileLocalIndex))
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Ldfld(nameof(Combat.stuff)),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(AMissileHit.worldX)),
					ILMatches.Call("Remove"),
					ILMatches.Instruction(OpCodes.Pop),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloc, missileLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_PutStackBack))),
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

		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(action, "ChecksForStackedObjectId", out var checksForStackedObjectId))
		{
			checksForStackedObjectId = ObtainStackedObjectId(@object);
			ModEntry.Instance.Helper.ModData.SetModData(action, "ChecksForStackedObjectId", checksForStackedObjectId);
		}

		if (checksForStackedObjectId == ObtainStackedObjectId(@object))
			return true;

		action.timer -= g.dt;
		return false;
	}

	private static void AMissileHit_Update_Transpiler_PutStackBack(AMissileHit action, Combat combat, Missile missile)
	{
		combat.stuff[action.worldX] = missile;
		RemoveStackedObject(combat, action.worldX, missile);
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
		
		ApplyToAllStackedObjects(combat, @object =>
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
		
		if (AnyStackedObject(combat, @object => @object is JupiterDrone))
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
		
		List<(int WorldX, AAttack Attack, StuffBase TopObject, StuffBase? StackedObject, int Depth)> attacks = [];
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
			List<(int WorldX, StuffBase TopObject, JupiterDrone JupiterDrone, int Depth)> stackedJupitedDrones = [];

			foreach (var kvp in c.stuff)
				if (GetStackedObjects(kvp.Value) is { } stackedObjects)
					for (var i = 0; i < stackedObjects.Count; i++)
						if (stackedObjects[i] is JupiterDrone jupiterDrone)
							stackedJupitedDrones.Add((kvp.Key, kvp.Value, jupiterDrone, i + 1));
				
			c.stuff = [];

			while (stackedJupitedDrones.Count != 0)
			{
				var entry = stackedJupitedDrones[^1];
				stackedJupitedDrones.RemoveAt(stackedJupitedDrones.Count - 1);
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

					var stackedObjectId = ObtainStackedObjectId(entry.JupiterDrone);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceStackedObjectId", stackedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceStackedObjectWorldX", entry.WorldX);
					
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

		if (AnyStackedObject(combat, @object => @object is JupiterDrone))
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
			if (GetStackedObjects(kvp.Value) is { } stackedObjects)
				__state[kvp.Key] = stackedObjects;
	}

	private static void AMedusaField_Begin_Postfix(Combat c, in Dictionary<int, List<StuffBase>> __state)
	{
		foreach (var kvp in __state)
		{
			var stackedObjects = kvp.Value.Select(StuffBase (stackedObject) => new Geode
			{
				x = stackedObject.x,
				xLerped = stackedObject.xLerped,
				bubbleShield = stackedObject.bubbleShield,
				targetPlayer = stackedObject.targetPlayer,
				age = stackedObject.age,
			}).ToList();

			if (c.stuff.TryGetValue(kvp.Key, out var @object))
			{
				SetStackedObjects(@object, stackedObjects);
			}
			else
			{
				foreach (var stackedObject in stackedObjects)
					PushStackedObject(c, kvp.Key, stackedObject);
			}
		}
	}

	private static void AMedusaField_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllStackedObjects(combat, @object =>
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
			if (GetStackedObjects(kvp.Value) is not { } stackedObjects)
				continue;
			__state[kvp.Key] = stackedObjects;
			SetStackedObjects(kvp.Value, null);
		}
	}

	private static void ASlurpMidrowObject_Begin_Finalizer(Combat c, in Dictionary<int, List<StuffBase>> __state)
	{
		foreach (var kvp in __state)
		{
			if (c.stuff.TryGetValue(kvp.Key, out var @object))
			{
				SetStackedObjects(@object, kvp.Value);
			}
			else
			{
				foreach (var stackedObject in kvp.Value)
					PushStackedObject(c, kvp.Key, stackedObject);
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
		ApplyToAllStackedObjects(c, @object =>
		{
			@object.bubbleShield = true;
		});
	}

	private static void ABubbleField_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllStackedObjects(combat, @object =>
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
		
		ApplyToAllStackedObjects(combat, @object =>
		{
			if (@object.GetActions(s, combat) is not null)
				@object.hilight = 2;
		});
	}
	#endregion
}

internal static class StackExtensions
{
	public static ASpawn SetStacked(this ASpawn action, bool value = true)
	{
		Stack.SetStacked(action, value);
		return action;
	}
	
	public static StuffBase SetWobbly(this StuffBase thing, bool value = true)
	{
		Stack.SetWobbly(thing, value);
		return thing;
	}
}