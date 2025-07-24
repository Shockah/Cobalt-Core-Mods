using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
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
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.GetSections)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_GetSections_Postfix))
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

	private static void ArtifactBrowse_GetSections_Postfix(State state, ref IEnumerable<ArtifactBrowse.Section> __result)
	{
		var sections = __result.ToList();
		
		var colorlessIndex = sections.FindIndex(e => e.title() == Character.GetDisplayName(Deck.colorless, state));
		if (colorlessIndex == -1)
			return;
		
		var starterShipArtifactTypes = StarterShip.ships.Values
			.SelectMany(ship => ship.artifacts)
			.Select(a => a.GetType())
			.ToHashSet();
		var exclusiveShipArtifactTypes = StarterShip.ships.Keys
			.SelectMany(shipKey => ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(shipKey)?.Configuration.ExclusiveArtifactTypes ?? new HashSet<Type>())
			.ToHashSet();

		var shipArtifacts = sections[colorlessIndex].artifacts
			.Where(a => starterShipArtifactTypes.Contains(a.GetType()) || exclusiveShipArtifactTypes.Contains(a.GetType()))
			.ToList();
		var eventArtifacts = sections[colorlessIndex].artifacts
			.Where(a => a.GetMeta().pools.Contains(ArtifactPool.EventOnly))
			.Where(a => !shipArtifacts.Contains(a))
			.Where(a => a is not HARDMODE)
			.ToList();
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.EventCategory)
		{
			sections[colorlessIndex].artifacts.RemoveAll(a => eventArtifacts.Contains(a));
			sections.Insert(colorlessIndex + 1, new() { title = () => ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Category", "Event"]), artifacts = eventArtifacts });
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.ExtraArtifactCodexCategories.ShipCategory)
		{
			sections[colorlessIndex].artifacts.RemoveAll(a => shipArtifacts.Contains(a));
			sections.Insert(colorlessIndex + 1, new() { title = () => ModEntry.Instance.Localizations.Localize(["ExtraArtifactCodexCategories", "Category", "Ship"]), artifacts = shipArtifacts });
		}

		__result = sections;
	}
}