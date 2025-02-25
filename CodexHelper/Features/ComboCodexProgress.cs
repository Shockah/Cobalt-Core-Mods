using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CodexHelper;

internal sealed partial class ProfileSettings
{
	[JsonProperty] public bool ShowBeatenDifficulties = true;
}

internal sealed class ComboCodexProgress : IRegisterable
{
	private static ISpriteEntry DoneIcon = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		DoneIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ComboDone.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.DifficultyOptions)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_DifficultyOptions_Postfix_Last)), priority: Priority.Last)
		);
	}
	
	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => $"<c=white>{ModEntry.Instance.Localizations.Localize(["comboCodexProgress", "settings", "header"])}</c>"
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["comboCodexProgress", "settings", nameof(ProfileSettings.ShowBeatenDifficulties), "title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.ShowBeatenDifficulties,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.ShowBeatenDifficulties = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.ShowBeatenDifficulties)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["comboCodexProgress", "settings", nameof(ProfileSettings.ShowBeatenDifficulties), "title"]),
					Description = ModEntry.Instance.Localizations.Localize(["comboCodexProgress", "settings", nameof(ProfileSettings.ShowBeatenDifficulties), "description"]),
				}
			]),
		]);
	
	private static void NewRunOptions_DifficultyOptions_Postfix_Last(G g, RunConfig runConfig)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.ShowBeatenDifficulties)
			return;
		
		var selectedCharKeys = runConfig.selectedChars.Select(d => d.Key()).ToHashSet();
		var maxBeatenDifficulty = g.state.bigStats.combos
			.Select(kvp => (Combo: BigStats.ParseComboKey(kvp.Key), Stats: kvp.Value))
			.Where(e => e.Combo is not null)
			.Select(e => (Combo: e.Combo!.Value, Stats: e.Stats))
			.Where(e =>
			{
				// this won't matter for vanilla, but if a mod comes out that makes 2-crew runs possible, this will behave correctly now
				if (runConfig.IsValid(g))
					return selectedCharKeys.SetEquals(e.Combo.decks.Select(d => d.Key()));
				else
					return !selectedCharKeys.Except(e.Combo.decks.Select(d => d.Key())).Any();
			})
			.Select(e => e.Stats.maxDifficultyWin ?? int.MinValue)
			.DefaultIfEmpty(int.MinValue)
			.Max();

		for (var i = 0; i < NewRunOptions.difficulties.Count; i++)
		{
			if (maxBeatenDifficulty < NewRunOptions.difficulties[i].level)
				continue;
			if (g.boxes.LastOrDefault(b => b.key?.k == StableUK.newRun_difficulty && b.key?.v == i) is not { } box)
				continue;
			
			Draw.Sprite(DoneIcon.Sprite, box.rect.x2 - 6, box.rect.y + box.rect.h / 2 - 5);
			
			if (!box.IsHover())
				continue;
			
			g.tooltips.tooltips.Insert(0, new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(ComboCodexProgress)}")
			{
				Icon = DoneIcon.Sprite,
				TitleColor = Colors.textChoice,
				Title = ModEntry.Instance.Localizations.Localize(["comboCodexProgress", "Done", "title"]),
				Description = ModEntry.Instance.Localizations.Localize(["comboCodexProgress", "Done", "description", runConfig.selectedChars.Count == 0 ? "anyCrew" : "specificCrew"]),
			});
		}
	}
}