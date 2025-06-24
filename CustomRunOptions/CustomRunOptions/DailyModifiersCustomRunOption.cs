﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal sealed class DailyModifiersCustomRunOption : ICustomRunOption
{
	private static readonly Lazy<List<Artifact>> DailyArtifacts = new(
		() => DB.artifacts
			.Where(kvp => kvp.Value != typeof(DailyJustOneCharacter) && kvp.Value != typeof(DailyModeTracker))
			.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.artifactMetas[kvp.Key]))
			.Where(e => e.Meta.pools.Contains(ArtifactPool.DailyOnly))
			.Select(e => (Artifact)Activator.CreateInstance(e.Type)!)
			.OrderBy(a => a.GetLocName())
			.ToList()
	);
	
	private static readonly Lazy<List<Artifact>> HullModArtifacts = new(
		() => DailyArtifacts.Value
			.Where(a => a.GetMeta().pools.Contains(ArtifactPool.DailyHullMod))
			.ToList()
	);
	
	private static readonly Lazy<List<Artifact>> StarterDeckModArtifacts = new(
		() => DailyArtifacts.Value
			.Where(a => a.GetMeta().pools.Contains(ArtifactPool.DailyStarterDeckMod))
			.ToList()
	);
	
	private static readonly Lazy<List<Artifact>> CardUpgradesModArtifacts = new(
		() => DailyArtifacts.Value
			.Where(a => a.GetMeta().pools.Contains(ArtifactPool.DailyCardUpgradesMod))
			.ToList()
	);

	private static readonly Lazy<List<Artifact>> SortedDailyArtifacts = new(() => [
		// DailyArtifacts.Value.First(a => a is DailyModeTracker),
		.. DailyArtifacts.Value.Where(a => a is not DailyModeTracker && !HullModArtifacts.Value.Contains(a) && !StarterDeckModArtifacts.Value.Contains(a) && !CardUpgradesModArtifacts.Value.Contains(a)),
		.. HullModArtifacts.Value,
		.. StarterDeckModArtifacts.Value,
		.. CardUpgradesModArtifacts.Value,
	]);
	
	internal static readonly Lazy<HashSet<string>> HullModArtifactKeySet = new(HullModArtifacts.Value.Select(a => a.Key()).ToHashSet);
	internal static readonly Lazy<HashSet<string>> StarterDeckModArtifactKeySet = new(StarterDeckModArtifacts.Value.Select(a => a.Key()).ToHashSet);
	internal static readonly Lazy<HashSet<string>> CardUpgradesModArtifactKeySet = new(CardUpgradesModArtifacts.Value.Select(a => a.Key()).ToHashSet);
	
	internal static readonly Lazy<List<string>> SortedDailyArtifactKeys = new(() => SortedDailyArtifacts.Value.Select(a => a.Key()).ToList());
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Postfix))
		);
	}

	public IReadOnlyList<ICustomRunOption.INewRunOptionsElement> GetNewRunOptionsElements(G g, RunConfig config)
		=> SortedDailyArtifacts.Value
			.Where(a => config.IsDailyModifierSelected(a.Key()))
			.Select(a => new ArtifactNewRunOptionsElement(a))
			.ToList();

	public IModSettingsApi.IModSetting MakeCustomRunSettings(IPluginPackage<IModManifest> package, IModSettingsApi api, RunConfig config)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => $"<c=white>{ModEntry.Instance.Localizations.Localize(["options", nameof(DailyModifiersCustomRunOption), "title"])}</c>"
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeList(
				SortedDailyArtifacts.Value
					.Select(IModSettingsApi.IModSetting (a) => new IconAffixModSetting
					{
						Setting = api.MakeCheckbox(
							a.GetLocName,
							() => config.IsDailyModifierSelected(a.Key()),
							(_, _, value) => config.SetDailyModifierSelected(a.Key(), value)
						).SetTooltips(a.GetTooltips),
						LeftIcon = new() { Icon = a.GetSprite() },
					})
					.ToList()
			)
		]);

	private static void State_PopulateRun_Postfix(State __instance)
	{
		foreach (var artifact in SortedDailyArtifacts.Value)
			if (__instance.runConfig.IsDailyModifierSelected(artifact.Key()))
				__instance.SendArtifactToChar(Mutil.DeepCopy(artifact));
	}
}

file static class RunConfigExt
{
	public static bool IsDailyModifierSelected(this RunConfig config, string key)
		=> ModEntry.Instance.Helper.ModData.TryGetModData<List<string>>(config, "DailyModifiers", out var dailyModifierKeys) && dailyModifierKeys.Contains(key);

	public static void SetDailyModifierSelected(this RunConfig config, string key, bool isSelected)
	{
		var dailyModifierKeys = ModEntry.Instance.Helper.ModData.ObtainModData<List<string>>(config, "DailyModifiers");
		dailyModifierKeys.Remove(key);

		if (!isSelected)
			return;

		if (DailyModifiersCustomRunOption.HullModArtifactKeySet.Value.Contains(key))
			dailyModifierKeys.RemoveAll(k => DailyModifiersCustomRunOption.HullModArtifactKeySet.Value.Contains(k));
		if (DailyModifiersCustomRunOption.StarterDeckModArtifactKeySet.Value.Contains(key))
			dailyModifierKeys.RemoveAll(k => DailyModifiersCustomRunOption.StarterDeckModArtifactKeySet.Value.Contains(k));
		if (DailyModifiersCustomRunOption.CardUpgradesModArtifactKeySet.Value.Contains(key))
			dailyModifierKeys.RemoveAll(k => DailyModifiersCustomRunOption.CardUpgradesModArtifactKeySet.Value.Contains(k));
			
		dailyModifierKeys.Add(key);

		var sorted = dailyModifierKeys.OrderBy(k => DailyModifiersCustomRunOption.SortedDailyArtifactKeys.Value.IndexOf(k)).ToList();
		dailyModifierKeys.Clear();
		dailyModifierKeys.AddRange(sorted);
	}
}