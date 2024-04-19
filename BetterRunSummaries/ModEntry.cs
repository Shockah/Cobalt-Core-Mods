using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.BetterRunSummaries;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;

	private static State? LastSavingState;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);

		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Postfix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(RunSummary), nameof(RunSummary.SaveFromState)),
			prefix: new HarmonyMethod(GetType(), nameof(RunSummary_SaveFromState_Prefix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => typeof(RunSummary).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<SaveFromState>") && m.ReturnType == typeof(CardSummary)),
			postfix: new HarmonyMethod(GetType(), nameof(RunSummary_SaveFromState_CardSelectDelegate_Postfix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(RunSummaryRoute), nameof(RunSummaryRoute.Render)),
			transpiler: new HarmonyMethod(GetType(), nameof(RunSummaryRoute_Render_Transpiler))
		);
	}

	private static void Combat_TryPlayCard_Postfix(Card card, bool __result)
	{
		if (__result)
			card.IncrementTimesPlayed();
	}

	private static void RunSummary_SaveFromState_Prefix(State s)
		=> LastSavingState = s;

	private static void RunSummary_SaveFromState_CardSelectDelegate_Postfix(Card c, ref CardSummary __result)
	{
		if (LastSavingState is not { } state)
			return;

		__result.SetTimesPlayed(c.GetTimesPlayed());
		__result.SetTraitOverrides(
			Instance.Helper.Content.Cards.GetAllCardTraits(state, c)
				.Where(kvp => kvp.Value.PermanentOverride is not null)
				.Select(kvp => new KeyValuePair<ICardTraitEntry, bool>(kvp.Key, kvp.Value.PermanentOverride!.Value))
		);
	}

	private static IEnumerable<CodeInstruction> RunSummaryRoute_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<Card>(originalMethod).CreateLdlocInstruction(out var ldlocCard),
					ILMatches.Ldloc<CardSummary>(originalMethod).CreateLdlocInstruction(out var ldlocCardSummary),
					ILMatches.Ldfld("upgrade"),
					ILMatches.Stfld("upgrade")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocCard,
					ldlocCardSummary,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler_ApplyExtraData)))
				)
				.Find(
					ILMatches.Ldloc<Card>(originalMethod).CreateLdlocInstruction(out ldlocCard),
					ILMatches.Call("GetFullDisplayName")
				)
				.Find(ILMatches.Call("Text"))
				.Replace(
					ldlocCard,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler_HijackCardRender)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void RunSummaryRoute_Render_Transpiler_ApplyExtraData(Card card, CardSummary cardSummary)
	{
		var fakeState = Mutil.DeepCopy(DB.fakeState);
		card.SetTimesPlayed(cardSummary.GetTimesPlayed() ?? 0);
		foreach (var traitOverride in cardSummary.GetTraitOverrides())
			Instance.Helper.Content.Cards.SetCardTraitOverride(fakeState, card, traitOverride.Key, traitOverride.Value, permanent: true);
	}

	private static Rect RunSummaryRoute_Render_Transpiler_HijackCardRender(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale, Card card)
	{
		var traitIndex = 0;
		foreach (var trait in Instance.Helper.Content.Cards.GetAllCardTraits(DB.fakeState, card))
		{
			if (trait.Value.PermanentOverride is not { } permanentOverride)
				continue;
			if (trait.Key.Configuration.Icon(DB.fakeState, card) is not { } icon)
				continue;

			if (!dontDraw)
			{
				Draw.Sprite(icon, x + traitIndex * 10, y - 2);
				if (!permanentOverride)
					Draw.Sprite(StableSpr.icons_unplayable, x + traitIndex * 10, y - 2);
			}

			str = $"{Tooltip.GetIndent()}{str}";
			traitIndex++;
		}

		if (card.GetTimesPlayed() is { } timesPlayed)
			str = $"{str} <c=white>({timesPlayed})</c>";

		return Draw.Text(str, x, y, font, color, colorForce, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
	}
}