using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nickel;
using Shockah.Shared;

namespace Shockah.CodexHelper;

internal sealed partial class ProfileSettings
{
	[JsonProperty] public bool TrackArtifactTakenCompletion = true;
	[JsonProperty] public bool TrackArtifactSeenCompletion = true;
	[JsonProperty] public ArtifactProgressDisplayStyleWhilePickingEnum ArtifactProgressDisplayStyleWhilePicking = ArtifactProgressDisplayStyleWhilePickingEnum.Icon;

	[JsonConverter(typeof(StringEnumConverter))]
	public enum ArtifactProgressDisplayStyleWhilePickingEnum
	{
		Text, Icon, Both
	}
}

internal static class StateVarsArtifactExt
{
	public static bool IsArtifactSeen(this StoryVars storyVars, string key, ArtifactReward? route, bool isTaken = false)
	{
		if (isTaken)
			return true;
		
		var newlySeenArtifacts = route is null ? null : ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(route, "NewlySeenArtifacts");
		if (newlySeenArtifacts is not null && newlySeenArtifacts.Contains(key))
			return false;
		
		var seenArtifacts = ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(storyVars, "SeenArtifacts");
		return seenArtifacts.Contains(key);
	}

	public static void SetArtifactSeen(this StoryVars storyVars, string key, ArtifactReward? route, bool isSeen = true)
	{
		var newlySeenArtifacts = route is null ? null : ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(route, "NewlySeenArtifacts");
		var seenArtifacts = ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(storyVars, "SeenArtifacts");

		if (isSeen)
		{
			if (!seenArtifacts.Contains(key) && newlySeenArtifacts is not null)
				newlySeenArtifacts.Add(key);
			seenArtifacts.Add(key);
		}
		else
		{
			seenArtifacts.Remove(key);
		}
	}
}

internal sealed class ArtifactCodexProgress : IRegisterable
{
	private static ISpriteEntry NewIcon = null!;
	private static ISpriteEntry SeenIcon = null!;
	private static ISpriteEntry TakenIcon = null!;
	
	private static ArtifactBrowse? RenderedArtifactBrowse;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		NewIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ArtifactNew.png"));
		SeenIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ArtifactSeen.png"));
		TakenIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ArtifactTaken.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactReward_Render_Postfix_Last)), priority: Priority.Last)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactReward_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Render)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Postfix_Last)), priority: Priority.Last)
		);
	}
	
	private static void ArtifactReward_Render_Postfix_Last(ArtifactReward __instance, G g)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "MarkedArtifactsAsSeen"))
		{
			foreach (var artifact in __instance.artifacts)
				g.state.storyVars.SetArtifactSeen(artifact.Key(), __instance);
			ModEntry.Instance.Helper.ModData.SetModData(__instance, "MarkedArtifactsAsSeen", true);
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.ArtifactProgressDisplayStyleWhilePicking == ProfileSettings.ArtifactProgressDisplayStyleWhilePickingEnum.Text)
			return;

		for (var i = 0; i < __instance.artifacts.Count; i++)
		{
			if (g.boxes.LastOrDefault(b => b.key?.k == StableUK.artifactReward_artifact && b.key?.v == i) is not { } box)
				continue;
			
			switch (ModEntry.Instance.Api.GetArtifactProgress(g.state, __instance.artifacts[i].Key(), __instance))
			{
				case ICodexHelperApi.IArtifactProgress.NotSeen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion || ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
					RenderIcon(ICodexHelperApi.IArtifactProgress.NotSeen);
					continue;
				case ICodexHelperApi.IArtifactProgress.Seen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
					RenderIcon(ICodexHelperApi.IArtifactProgress.Seen);
					continue;
				case ICodexHelperApi.IArtifactProgress.Seen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion:
					RenderIcon(ICodexHelperApi.IArtifactProgress.Seen, NewIcon);
					continue;
				case ICodexHelperApi.IArtifactProgress.Taken:
				default:
					break;
			}
			
			void RenderIcon(ICodexHelperApi.IArtifactProgress progress, ISpriteEntry? iconOverride = null)
			{
				var icon = iconOverride ?? progress switch
				{
					ICodexHelperApi.IArtifactProgress.NotSeen => NewIcon,
					ICodexHelperApi.IArtifactProgress.Seen => SeenIcon,
					ICodexHelperApi.IArtifactProgress.Taken => TakenIcon,
					_ => throw new ArgumentOutOfRangeException(nameof(progress), progress, null)
				};
			
				Draw.Sprite(icon.Sprite, box.rect.x2 - 6, box.rect.y + box.rect.h / 2 - 5 + (box.IsHover() ? 1 : 0));
			
				if (!box.IsHover())
					return;
			
				g.tooltips.tooltips.Insert(0, new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(ArtifactCodexProgress)}")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["artifactCodexProgress", progress.ToString(), "title"]),
					Description = ModEntry.Instance.Localizations.Localize(["artifactCodexProgress", progress.ToString(), "description"]),
				});
			}
		}
	}

	private static void ArtifactBrowse_Render_Prefix(ArtifactBrowse __instance)
		=> RenderedArtifactBrowse = __instance;

	private static void ArtifactBrowse_Render_Finalizer()
		=> RenderedArtifactBrowse = null;
	
	private static IEnumerable<CodeInstruction> ArtifactBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("g"),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("persistentStoryVars"),
					ILMatches.Ldfld("artifactsOwned"),
					ILMatches.Ldloca<KeyValuePair<string, Type>>(originalMethod).CreateLdlocInstruction(out var ldlocKvp),
					ILMatches.Call("get_Key"),
					ILMatches.Call("Contains"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocKvp,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler_ModifyUnknown))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool ArtifactBrowse_Render_Transpiler_ModifyUnknown(bool isKnown, G g, KeyValuePair<string, Type> kvp)
	{
		if (isKnown)
			return true;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.TrackArtifactSeenCompletion)
			return false;
		if (!g.state.persistentStoryVars.IsArtifactSeen(kvp.Key, null))
			return false;
		return true;
	}
	
	private static IEnumerable<CodeInstruction> ArtifactReward_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldfld("artifacts"),
					ILMatches.AnyLdloc,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<Artifact>(originalMethod).CreateLdlocInstruction(out var ldlocArtifact)
				)
				.Find(
					ILMatches.Ldstr("artifactReward.bossArtifactSuffix"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T"),
					ILMatches.Call("Concat")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocArtifact,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactReward_Render_Transpiler_ModifySubtitleIfNeeded)))
				)
				.Find(ILMatches.Call("Text"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactReward_Render_Transpiler_UnforceColorText))))
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
	
	private static string ArtifactReward_Render_Transpiler_ModifySubtitleIfNeeded(string subtitle, Artifact artifact, ArtifactReward route, G g)
	{
		if (ModEntry.Instance.Settings.ProfileBased.Current.ArtifactProgressDisplayStyleWhilePicking == ProfileSettings.ArtifactProgressDisplayStyleWhilePickingEnum.Icon)
			return subtitle;
		
		switch (ModEntry.Instance.Api.GetArtifactProgress(g.state, artifact.Key(), route))
		{
			case ICodexHelperApi.IArtifactProgress.NotSeen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion || ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
				return GetModifiedText(ICodexHelperApi.ICardProgress.NotSeen);
			case ICodexHelperApi.IArtifactProgress.Seen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion || ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
				return GetModifiedText(ICodexHelperApi.ICardProgress.Seen);
			case ICodexHelperApi.IArtifactProgress.Taken:
			default:
				return subtitle;
		}
		
		string GetModifiedText(ICodexHelperApi.ICardProgress progress)
		{
			var strippedText = TextParserExt.StripColorsFromText(subtitle);
			var extra = ModEntry.Instance.Localizations.Localize(["artifactCodexProgress", progress.ToString(), "artifactSubtitle"]);
			return progress switch
			{
				ICodexHelperApi.ICardProgress.NotSeen => $"<c=textBold>{extra}</c> {strippedText}",
				ICodexHelperApi.ICardProgress.Seen => $"<c=textMain>{extra}</c> {strippedText}",
				ICodexHelperApi.ICardProgress.Taken => $"<c=textFaint>{extra}</c> {strippedText}",
				_ => throw new ArgumentOutOfRangeException(nameof(progress), progress, null)
			};
		}
	}

	private static Rect ArtifactReward_Render_Transpiler_UnforceColorText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale)
		=> Draw.Text(str, x, y, font, colorForce ?? color, null, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
	
	private static void Artifact_Render_Postfix_Last(Artifact __instance, G g, bool showAsUnknown)
	{
		if (showAsUnknown)
			return;
		if (RenderedArtifactBrowse is null)
			return;
		if (g.boxes.LastOrDefault(b => b.key?.k == StableUK.artifact) is not { } box)
			return;
			
		var progress = ModEntry.Instance.Api.GetArtifactProgress(g.state, __instance.Key());
		if (progress == ICodexHelperApi.IArtifactProgress.Seen && ModEntry.Instance.Settings.ProfileBased.Current.TrackArtifactSeenCompletion)
			RenderIcon(ICodexHelperApi.IArtifactProgress.Seen);
			
		void RenderIcon(ICodexHelperApi.IArtifactProgress progress, ISpriteEntry? iconOverride = null)
		{
			var icon = iconOverride ?? progress switch
			{
				ICodexHelperApi.IArtifactProgress.NotSeen => NewIcon,
				ICodexHelperApi.IArtifactProgress.Seen => SeenIcon,
				ICodexHelperApi.IArtifactProgress.Taken => TakenIcon,
				_ => throw new ArgumentOutOfRangeException(nameof(progress), progress, null)
			};
			
			Draw.Sprite(icon.Sprite, box.rect.x + 7, box.rect.y - 4);
			
			if (!box.IsHover())
				return;
			
			g.tooltips.tooltips.Insert(0, new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(ArtifactCodexProgress)}")
			{
				Icon = icon.Sprite,
				TitleColor = Colors.textChoice,
				Title = ModEntry.Instance.Localizations.Localize(["artifactCodexProgress", progress.ToString(), "title"]),
				Description = ModEntry.Instance.Localizations.Localize(["artifactCodexProgress", progress.ToString(), "description"]),
			});
		}
	}
}