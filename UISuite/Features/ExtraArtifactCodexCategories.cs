using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public ExtraArtifactCodexCategoriesSettings ExtraArtifactCodexCategories = new();

	internal sealed class ExtraArtifactCodexCategoriesSettings
	{
		[JsonProperty]
		public bool ShipCategory = true;
		
		[JsonProperty]
		public bool EventCategory = true;
	}
}

internal sealed class ExtraArtifactCodexCategories : IRegisterable
{
	private static ArtifactBrowse? LastRoute;
	private static bool RenderingArtifactBrowse;
	private static readonly List<string> ShipArtifactKeys = [];
	private static readonly List<string> EventArtifactKeys = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetDisplayName), [typeof(string), typeof(State)]),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Character_GetDisplayName_Postfix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "ShipCategory", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.ShipCategory,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.ShipCategory = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.ExtraArtifactCodexCategories)}::{nameof(ProfileSettings.ExtraArtifactCodexCategories.ShipCategory)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "ShipCategory", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "ShipCategory", "Description"]),
				},
			]),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "EventCategory", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.EventCategory,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.EventCategory = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.ExtraArtifactCodexCategories)}::{nameof(ProfileSettings.ExtraArtifactCodexCategories.EventCategory)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "EventCategory", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Settings", "EventCategory", "Description"]),
				},
			]),
		]);
	
	private static void ArtifactBrowse_Render_Prefix()
		=> RenderingArtifactBrowse = true;

	private static void ArtifactBrowse_Render_Finalizer()
		=> RenderingArtifactBrowse = false;

	private static IEnumerable<CodeInstruction> ArtifactBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stloc<List<(Deck, List<KeyValuePair<string, Type>>)>>(originalMethod))
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler_ModifyArtifacts)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<(Deck, List<KeyValuePair<string, Type>>)> ArtifactBrowse_Render_Transpiler_ModifyArtifacts(List<(Deck, List<KeyValuePair<string, Type>>)> allArtifacts, ArtifactBrowse route)
	{
		var colorlessIndex = allArtifacts.FindIndex(e => e.Item1 == Deck.colorless);
		if (colorlessIndex == -1)
			return allArtifacts;
		
		if (route != LastRoute)
		{
			LastRoute = route;

			var starterShipArtifacts = StarterShip.ships.Values
				.SelectMany(ship => ship.artifacts)
				.Select(a => a.GetType())
				.ToHashSet();
			var exclusiveShipArtifacts = StarterShip.ships.Keys
				.SelectMany(shipKey => ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(shipKey)?.Configuration.ExclusiveArtifactTypes ?? new HashSet<Type>())
				.ToHashSet();
			
			ShipArtifactKeys.Clear();
			ShipArtifactKeys.AddRange(
				allArtifacts[colorlessIndex].Item2
					.Where(kvp => starterShipArtifacts.Contains(kvp.Value) || exclusiveShipArtifacts.Contains(kvp.Value))
					.Select(kvp => kvp.Key)
			);

			EventArtifactKeys.Clear();
			EventArtifactKeys.AddRange(
				allArtifacts[colorlessIndex].Item2
					.Where(kvp => DB.artifactMetas[kvp.Key].pools.Contains(ArtifactPool.EventOnly))
					.Where(kvp => !ShipArtifactKeys.Contains(kvp.Key))
					.Where(kvp => kvp.Value != typeof(HARDMODE))
					.Select(kvp => kvp.Key)
			);
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.EventCategory)
		{
			allArtifacts[colorlessIndex].Item2.RemoveAll(kvp => EventArtifactKeys.Contains(kvp.Key));
			allArtifacts.Insert(colorlessIndex + 1, (Deck.ephemeral, EventArtifactKeys.ToDictionary(k => k, k => DB.artifacts[k]).ToList()));
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.ShipCategory)
		{
			allArtifacts[colorlessIndex].Item2.RemoveAll(kvp => ShipArtifactKeys.Contains(kvp.Key));
			allArtifacts.Insert(colorlessIndex + 1, (Deck.ares, ShipArtifactKeys.ToDictionary(k => k, k => DB.artifacts[k]).ToList()));
		}

		return allArtifacts;
	}

	private static void Character_GetDisplayName_Postfix(string charId, ref string __result)
	{
		if (!RenderingArtifactBrowse)
			return;

		switch (charId)
		{
			case nameof(Deck.ares):
				if (ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.ShipCategory)
					__result = ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Category", "Ship"]);
				break;
			case nameof(Deck.ephemeral):
				if (ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.EventCategory)
					__result = ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Category", "Event"]);
				break;
		}
	}
}