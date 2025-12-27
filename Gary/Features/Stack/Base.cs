using System;
using System.Collections.Generic;
using System.Linq;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Gary;

internal sealed partial class Stack : IRegisterable
{
	private static readonly Stack Instance = new();
	
	private static ISpriteEntry StackedIcon = null!;
	private static ISpriteEntry WobblyIcon = null!;
	private static ISpriteEntry StackedLaunchIcon = null!;

	private static StuffBase? ObjectBeingLaunchedInto;
	private static StuffBase? ObjectToPutLater;
	private static bool ObjectIsBeingStackedInto;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		StackedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/Stacked.png"));
		WobblyIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/Wobbly.png"));
		StackedLaunchIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/StackedLaunch.png"));

		HandleStatus();
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
	}

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