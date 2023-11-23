using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.CodexHelper;

public sealed class ModEntry : IModManifest
{
	private static ModEntry Instance = null!;

	public string Name { get; init; } = typeof(ModEntry).FullName!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	private static bool IsRenderingCardRewardScreen = false;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		harmony.TryPatch(
			logger: Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.Render)),
			prefix: new HarmonyMethod(GetType(), nameof(CardReward_Render_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(CardReward_Render_Finalizer))
		);
		harmony.TryPatch(
			logger: Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_Render_Transpiler))
		);
		harmony.TryPatch(
			logger: Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			transpiler: new HarmonyMethod(GetType(), nameof(ArtifactReward_Render_Transpiler))
		);
	}

	private static void CardReward_Render_Prefix()
		=> IsRenderingCardRewardScreen = true;

	private static void CardReward_Render_Finalizer()
		=> IsRenderingCardRewardScreen = false;

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldstr("cardRarity.common"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.Find(
					ILMatches.Ldstr("cardRarity.uncommon"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.Find(
					ILMatches.Ldstr("cardRarity.rare"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch methods - {Mod} probably won't work.\nReason: {Exception}", Instance.Name, ex);
			return instructions;
		}
	}

	private static string Card_Render_Transpiler_ModifyRarityTextIfNeeded(string rarityText, Card card, G g)
	{
		if (IsRenderingCardRewardScreen && !g.state.storyVars.cardsOwned.Contains(card.Key()))
			rarityText = $"<c=textMain>{I18n.MissingFromCodex}</c> {rarityText}";
		return rarityText;
	}

	private static IEnumerable<CodeInstruction> ArtifactReward_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldfld("artifacts"),
					ILMatches.AnyLdloc,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<Artifact>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocInstruction(out var ldlocArtifact)

				.Find(
					ILMatches.Ldstr("artifactReward.bossArtifactSuffix"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T"),
					ILMatches.Call("Concat")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocArtifact,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(ArtifactReward_Render_Transpiler_ModifySubtitleIfNeeded)))
				)

				.Find(ILMatches.Call("Text"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(ArtifactReward_Render_Transpiler_UnforceColorText))))

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch methods - {Mod} probably won't work.\nReason: {Exception}", Instance.Name, ex);
			return instructions;
		}
	}

	private static string ArtifactReward_Render_Transpiler_ModifySubtitleIfNeeded(string subtitle, Artifact artifact, G g)
	{
		subtitle = TextParserExt.StripColorsFromText(subtitle);
		if (!g.state.storyVars.artifactsOwned.Contains(artifact.Key()))
			subtitle = $"<c=textMain>{I18n.MissingFromCodex}</c> {subtitle}";
		return subtitle;
	}

	private static Rect ArtifactReward_Render_Transpiler_UnforceColorText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale)
		=> Draw.Text(str, x, y, font, colorForce ?? color, null, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
}
