using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal sealed class BootSequenceCustomRunOption : ICustomRunOptionsApi.ICustomRunOption, IRegisterable
{
	public const double PRIORITY = 50;
	
	public interface IBootChoice
	{
		string Key { get; }
		string Title { get; }
		
		bool Matches(Choice choice);

		public readonly record struct Vanilla(string Key) : IBootChoice
		{
			public string Title
				=> Loc.T(Key);

			public bool Matches(Choice choice)
				=> choice.label == Title;
		}

		public readonly record struct Modded(string Key, Func<string> TitleProvider, Func<Choice, bool> MatchPredicate) : IBootChoice
		{
			public string Title
				=> TitleProvider();
			
			public bool Matches(Choice choice)
				=> MatchPredicate(choice);
		}
	}

	internal static readonly Dictionary<string, IBootChoice> UpsideChoices = new List<string>
	{
		"BootSequence_Offer3Uncommon",
		"BootSequence_Offer3Rare",
		"BootSequence_CommonArtifact",
		"BootSequence_Upgrade",
		"BootSequence_Remove2Cards",
		"BootSequence_Gain1of3Common",
		"BootSequence_GainHull",
		"BootSequence_Upgrade2A",
		"BootSequence_Upgrade2B",
		"BootSequence_ReplaceWithBossArtifact",
	}.ToDictionary(k => k, k => (IBootChoice)new IBootChoice.Vanilla(k));
	
	internal static readonly Dictionary<string, IBootChoice> DownsideChoices = new List<string>
	{
		"BootSequenceDownsideDebris",
		"BootSequenceDownsideMaxHull",
		"BootSequenceDownsideMaxShield",
	}.ToDictionary(k => k, k => (IBootChoice)new IBootChoice.Vanilla(k));
	
	private static ISpriteEntry UpsideEnforceIcon = null!;
	private static ISpriteEntry UpsideBlacklistIcon = null!;
	private static ISpriteEntry DownsideEnforceIcon = null!;
	private static ISpriteEntry DownsideBlacklistIcon = null!;

	private static bool IsMidPopulateRun;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		UpsideEnforceIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BootSequenceUpsideEnforce.png"));
		UpsideBlacklistIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BootSequenceUpsideBlacklist.png"));
		DownsideEnforceIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BootSequenceDownsideEnforce.png"));
		DownsideBlacklistIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BootSequenceDownsideBlacklist.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.MakeRunStartTreats)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_MakeRunStartTreats_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_MakeRunStartTreats_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Dialogue), nameof(Dialogue.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Dialogue_Render_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Shout), nameof(Shout.GetChoices)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Shout_GetChoices_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Shout_GetChoices_Finalizer))
		);
		
		ModEntry.Instance.CustomRunOptions.Add(new BootSequenceCustomRunOption(), PRIORITY);
	}

	public IReadOnlyList<ICustomRunOptionsApi.INewRunOptionsElement> GetNewRunOptionsElements(G g, RunConfig config)
	{
		var elements = new List<ICustomRunOptionsApi.INewRunOptionsElement>();
		
		if (config.GetEnforcedBootSequenceUpside() is not null)
			elements.Add(new IconNewRunOptionsElement(UpsideEnforceIcon.Sprite));
		if (config.GetBlacklistedBootSequenceUpsides().Count != 0)
			elements.Add(new IconNewRunOptionsElement(UpsideBlacklistIcon.Sprite));
		
		if (config.GetEnforcedBootSequenceDownside() is not null)
			elements.Add(new IconNewRunOptionsElement(DownsideEnforceIcon.Sprite));
		if (config.GetBlacklistedBootSequenceDownsides().Count != 0)
			elements.Add(new IconNewRunOptionsElement(DownsideBlacklistIcon.Sprite));
		
		return elements;
	}

	public IModSettingsApi.IModSetting MakeCustomRunSettings(NewRunOptions baseRoute, G g, RunConfig config)
	{
		var api = ModEntry.Instance.ModSettingsApi;
		return api.MakeList([
			api.MakePadding(
				api.MakeText(() => $"<c=white>{ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "title", "upside"])}</c>"
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeButton(
				() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "enforce"]),
				(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
					api.MakeHeader(
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "title", "upside"]),
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "enforce"])
					),
					api.MakeList([
						api.MakeCheckbox(
							() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "none"]),
							() => config.GetEnforcedBootSequenceUpside() == "",
							(_, _, value) => config.SetEnforcedBootSequenceUpside(value ? "" : null)
						).SetTitleFont(() => DB.pinch).SetHeight(17),
						.. UpsideChoices.Values
							.Select(IModSettingsApi.IModSetting (choice) => api.MakeCheckbox(
								() => choice.Title,
								() => config.GetEnforcedBootSequenceUpside() == choice.Key,
								(_, _, value) => config.SetEnforcedBootSequenceUpside(value ? choice.Key : null)
							).SetTitleFont(() => DB.pinch).SetHeight(17))
					]),
					api.MakeBackButton()
				])))
			).SetValueText(() => config.GetEnforcedBootSequenceUpside() is { } key && UpsideChoices.TryGetValue(key, out var choice)
				? (key == "" ? ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "none"]) : choice.Title)
				: $"<c={Colors.textMain.gain(0.5)}>{ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "notSelected"])}</c>"
			).SetHeight(12).SetTitleFont(() => DB.pinch).SetValueTextFont(() => DB.pinch),
			api.MakeButton(
				() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "blacklist"]),
				(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
					api.MakeHeader(
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "title", "upside"]),
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "blacklist"])
					),
					api.MakeList(
						UpsideChoices.Values
							.Select(IModSettingsApi.IModSetting (choice) => api.MakeCheckbox(
								() => choice.Title,
								() => config.GetBlacklistedBootSequenceUpsides().Contains(choice.Key),
								(_, _, value) =>
								{
									if (config.GetEnforcedBootSequenceUpside() is not null)
										config.SetEnforcedBootSequenceUpside(null);
									if (value)
										config.GetBlacklistedBootSequenceUpsides().Add(choice.Key);
									else
										config.GetBlacklistedBootSequenceUpsides().Remove(choice.Key);
								}
							).SetTitleFont(() => DB.pinch).SetHeight(17))
							.ToList()
					),
					api.MakeBackButton()
				])))
			).SetValueText(() =>
			{
				var count = config.GetBlacklistedBootSequenceUpsides().Count;
				return count == 0 ? $"<c={Colors.textMain.gain(0.5)}>{ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "notSelected"])}</c>" : count.ToString();
			}).SetHeight(12).SetTitleFont(() => DB.pinch).SetValueTextFont(() => DB.pinch),

			api.MakePadding(
				api.MakeText(() => $"<c=white>{ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "title", "downside"])}</c>"
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeButton(
				() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "enforce"]),
				(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
					api.MakeHeader(
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "title", "downside"]),
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "enforce"])
					),
					api.MakeList([
						api.MakeCheckbox(
							() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "none"]),
							() => config.GetEnforcedBootSequenceDownside() == "",
							(_, _, value) => config.SetEnforcedBootSequenceDownside(value ? "" : null)
						).SetTitleFont(() => DB.pinch).SetHeight(17),
						.. DownsideChoices.Values
							.Select(IModSettingsApi.IModSetting (choice) => api.MakeCheckbox(
								() => choice.Title,
								() => config.GetEnforcedBootSequenceDownside() == choice.Key,
								(_, _, value) => config.SetEnforcedBootSequenceDownside(value ? choice.Key : null)
							).SetTitleFont(() => DB.pinch).SetHeight(17))
					]),
					api.MakeBackButton()
				])))
			).SetValueText(() => config.GetEnforcedBootSequenceDownside() is { } key && DownsideChoices.TryGetValue(key, out var choice)
				? (key == "" ? ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "none"]) : choice.Title)
				: $"<c={Colors.textMain.gain(0.5)}>{ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "notSelected"])}</c>"
			).SetHeight(12).SetTitleFont(() => DB.pinch).SetValueTextFont(() => DB.pinch),
			api.MakeButton(
				() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "blacklist"]),
				(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
					api.MakeHeader(
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "title", "downside"]),
						() => ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "blacklist"])
					),
					api.MakeList(
						DownsideChoices.Values
							.Select(IModSettingsApi.IModSetting (choice) => api.MakeCheckbox(
								() => choice.Title,
								() => config.GetBlacklistedBootSequenceDownsides().Contains(choice.Key),
								(_, _, value) =>
								{
									if (config.GetEnforcedBootSequenceDownside() is not null)
										config.SetEnforcedBootSequenceDownside(null);
									if (value)
										config.GetBlacklistedBootSequenceDownsides().Add(choice.Key);
									else
										config.GetBlacklistedBootSequenceDownsides().Remove(choice.Key);
								}
							).SetTitleFont(() => DB.pinch).SetHeight(17))
							.ToList()
					),
					api.MakeBackButton()
				])))
			).SetValueText(() =>
			{
				var count = config.GetBlacklistedBootSequenceDownsides().Count;
				return count == 0 ? $"<c={Colors.textMain.gain(0.5)}>{ModEntry.Instance.Localizations.Localize(["options", nameof(BootSequenceCustomRunOption), "notSelected"])}</c>" : count.ToString();
			}).SetHeight(12).SetTitleFont(() => DB.pinch).SetValueTextFont(() => DB.pinch)
		]);
	}

	private static void State_PopulateRun_Prefix()
		=> IsMidPopulateRun = true;

	private static void State_PopulateRun_Finalizer()
		=> IsMidPopulateRun = false;

	private static bool State_MakeRunStartTreats_Prefix(State __instance, ref Route __result)
	{
		if (!IsMidPopulateRun)
			return true;
		if (__instance.runConfig.GetEnforcedBootSequenceDownside() is null)
			return true;
		
		__result = Dialogue.MakeDialogueRouteOrSkip(__instance, DB.story.QuickLookup(__instance, "BootSequenceDownside"), OnDone.visitCurrent);
		return false;
	}

	private static void State_MakeRunStartTreats_Postfix(State __instance)
	{
		if (!IsMidPopulateRun)
			return;
		
		ModEntry.Instance.Helper.ModData.SetModData(__instance, "BootSequenceUpsideNeedsHandling", __instance.runConfig.GetEnforcedBootSequenceUpside() is not null || __instance.runConfig.GetBlacklistedBootSequenceUpsides().Count != 0);
		ModEntry.Instance.Helper.ModData.SetModData(__instance, "BootSequenceDownsideNeedsHandling", __instance.runConfig.GetEnforcedBootSequenceDownside() is not null || __instance.runConfig.GetBlacklistedBootSequenceDownsides().Count != 0);
	}

	private static void Dialogue_Render_Prefix(Dialogue __instance, G g)
	{
		if (__instance.ctx.script != "BootSequence" && __instance.ctx.script != "BootSequenceDownside")
			return;
		if (!__instance.IsTalkingStarted())
			return;
		if (__instance.shout is not { } shout)
			return;

		var isUpside = __instance.ctx.script == "BootSequence";
		var enforcedKey = isUpside ? g.state.runConfig.GetEnforcedBootSequenceUpside() : g.state.runConfig.GetEnforcedBootSequenceDownside();
		var blacklistedKeys = isUpside ? g.state.runConfig.GetBlacklistedBootSequenceUpsides() : g.state.runConfig.GetBlacklistedBootSequenceDownsides();

		var enforcedChoice = (string.IsNullOrEmpty(enforcedKey) ? null : (isUpside ? UpsideChoices : DownsideChoices).GetValueOrDefault(enforcedKey));
		var blacklistedChoices = blacklistedKeys.Select(k => (isUpside ? UpsideChoices : DownsideChoices).GetValueOrDefault(k)).OfType<IBootChoice>().ToHashSet();
		
		try
		{
			var attempt = 0;
			var maxAttempts = isUpside ? 1000 : 100;
			List<Choice> choices = [];
			List<Choice> allChoices = [];
			List<Choice>? originalChoices = null;
			var startingChoiceCount = -1;
			NextChoices();
			
			if (startingChoiceCount <= 0)
				return;

			var needsHandlingKey = isUpside ? "BootSequenceUpsideNeedsHandling" : "BootSequenceDownsideNeedsHandling";
			var needsHandling = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(g.state, needsHandlingKey);

			if (!needsHandling)
				return;
			ModEntry.Instance.Helper.ModData.RemoveModData(g.state, needsHandlingKey);

			while (attempt < maxAttempts)
			{
				attempt++;

				if (enforcedKey == "")
				{
					QueueFinish();
					return;
				}

				if (enforcedChoice is not null)
				{
					if (choices.FirstOrDefault(choice => enforcedChoice.Matches(choice)) is not { } choice)
					{
						NextChoices();
						continue;
					}

					__instance.InvokeChoice(g, choice);
					return;
				}

				if (allChoices.Count < startingChoiceCount)
				{
					NextChoices();
					continue;
				}

				break;
			}
			
			if (allChoices.Count == 0)
				QueueFinish();
			else
				ModEntry.Instance.Helper.ModData.SetModData(shout, "ChoicesOverride", allChoices.Take(startingChoiceCount).ToList());

			void NextChoices()
			{
				ModEntry.Instance.Helper.ModData.SetModData(shout, "RngCurrentEventSeedOverride", g.state.rngCurrentEvent.seed + attempt);
				shout._choicesCache = null;
				var newChoices = shout.GetChoices(g);
				if (startingChoiceCount < 0)
					startingChoiceCount = newChoices?.Count ?? 0;
				originalChoices ??= newChoices?.ToList();
				choices = newChoices ?? choices;
				choices.RemoveAll(choice => blacklistedChoices.Any(blacklistedChoice => blacklistedChoice.Matches(choice)));
				foreach (var newChoice in choices)
					if (allChoices.All(existingChoice => existingChoice.label != newChoice.label))
						allChoices.Add(newChoice);
			}

			void QueueFinish()
			{
				var finalChoices = originalChoices ?? choices;
				__instance.actionQueue.Queue(new AJumpScript { script = finalChoices.Count == 0 ? ".zone_first" : finalChoices[0].key });
			}
		}
		finally
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(shout, "RngCurrentEventSeedOverride");
		}
	}

	private static void Shout_GetChoices_Prefix(Shout __instance, G g, out uint __state)
	{
		__state = g.state.rngCurrentEvent.seed;
		g.state.rngCurrentEvent.seed = ModEntry.Instance.Helper.ModData.GetOptionalModData<uint>(__instance, "RngCurrentEventSeedOverride") ?? g.state.rngCurrentEvent.seed;
	}

	private static void Shout_GetChoices_Finalizer(Shout __instance, G g, ref List<Choice>? __result, in uint __state)
	{
		g.state.rngCurrentEvent.seed = __state;
		if (ModEntry.Instance.Helper.ModData.TryGetModData<List<Choice>>(__instance, "ChoicesOverride", out var choicesOverride))
			__result = choicesOverride;
	}
}

file static class RunConfigExt
{
	public static string? GetEnforcedBootSequenceUpside(this RunConfig config)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<string>(config, "EnforcedBootSequenceUpside");

	public static void SetEnforcedBootSequenceUpside(this RunConfig config, string? value)
	{
		ModEntry.Instance.Helper.ModData.RemoveModData(config, "BlacklistedBootSequenceUpsides");
		ModEntry.Instance.Helper.ModData.SetOptionalModData(config, "EnforcedBootSequenceUpside", value);
	}

	public static HashSet<string> GetBlacklistedBootSequenceUpsides(this RunConfig config)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(config, "BlacklistedBootSequenceUpsides");
	
	public static string? GetEnforcedBootSequenceDownside(this RunConfig config)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<string>(config, "EnforcedBootSequenceDownside");

	public static void SetEnforcedBootSequenceDownside(this RunConfig config, string? value)
	{
		ModEntry.Instance.Helper.ModData.RemoveModData(config, "BlacklistedBootSequenceDownsides");
		ModEntry.Instance.Helper.ModData.SetOptionalModData(config, "EnforcedBootSequenceDownside", value);
	}

	public static HashSet<string> GetBlacklistedBootSequenceDownsides(this RunConfig config)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(config, "BlacklistedBootSequenceDownsides");
}