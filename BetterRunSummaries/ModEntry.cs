using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Kokoro;
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
	internal readonly IKokoroApi.IV2? KokoroApi;
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	private static State? LastSavingState;
	private static RunSummaryRoute? LastRunSummaryRoute;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")?.V2;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			prefix: new HarmonyMethod(GetType(), nameof(State_PopulateRun_Prefix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Pulse)),
			postfix: new HarmonyMethod(GetType(), nameof(Artifact_Pulse_Postfix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Postfix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.PlayerLost)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_PlayerLost_Prefix))
		);
		Harmony.TryPatch(
			logger: Logger,
			original: () => AccessTools.DeclaredMethod(typeof(RunSummary), nameof(RunSummary.Save)),
			prefix: new HarmonyMethod(GetType(), nameof(RunSummary_Save_Prefix))
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
			prefix: new HarmonyMethod(GetType(), nameof(RunSummaryRoute_Render_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(RunSummaryRoute_Render_Postfix)),
			transpiler: new HarmonyMethod(GetType(), nameof(RunSummaryRoute_Render_Transpiler))
		);
	}

	private static void State_PopulateRun_Prefix(State __instance)
		=> Instance.Helper.ModData.RemoveModData(__instance, "TimesCardsPlayed");

	private static void Artifact_Pulse_Postfix(Artifact __instance)
		=> __instance.IncrementTimesTriggered();

	private static void Combat_TryPlayCard_Postfix(State s, Card card, bool __result)
	{
		if (__result)
			card.IncrementTimesPlayed(s);
	}

	private static void Combat_PlayerLost_Prefix(Combat __instance, G g)
		=> g.state.SetEnemyDiedTo(__instance.otherShip.ai?.Key());

	private static void RunSummary_Save_Prefix(RunSummary __instance)
	{
		if (LastSavingState is not { } state)
			return;

		__instance.SetEnemyDiedTo(state.GetEnemyDiedTo());
		state.SetEnemyDiedTo(null);

		__instance.SetTimesArtifactsTriggered(
			state.EnumerateAllArtifacts()
				.Select(a => new KeyValuePair<string, int?>(a.Key(), a.GetTimesTriggered()))
				.Where(kvp => kvp.Value is not null)
				.Select(kvp => new KeyValuePair<string, int>(kvp.Key, kvp.Value!.Value))
		);
	}

	private static void RunSummary_SaveFromState_Prefix(State s)
		=> LastSavingState = s;

	private static void RunSummary_SaveFromState_CardSelectDelegate_Postfix(Card c, ref CardSummary __result)
	{
		if (LastSavingState is not { } state)
			return;

		__result.SetTimesPlayed(c.GetTimesPlayed(state));
		__result.SetTraitOverrides(
			Instance.Helper.Content.Cards.GetAllCardTraits(state, c)
				.Where(kvp => kvp.Value.PermanentOverride is not null)
				.Where(kvp => kvp.Value.Innate != kvp.Value.PermanentOverride)
				.Select(kvp => new KeyValuePair<ICardTraitEntry, bool>(kvp.Key, kvp.Value.PermanentOverride!.Value))
		);
	}

	private static void RunSummaryRoute_Render_Prefix(RunSummaryRoute __instance)
	{
		LastRunSummaryRoute = __instance;
		if (!Instance.Helper.ModData.ContainsModData(__instance, "FakeState"))
			Instance.Helper.ModData.SetModData(__instance, "FakeState", Mutil.DeepCopy(DB.fakeState));
	}

	private static void RunSummaryRoute_Render_Postfix(RunSummaryRoute __instance)
	{
		// ReSharper disable UselessBinaryOperation
		// ReSharper disable RedundantAssignment
		var slideIn = Vault.GetSlideIn(__instance.introAnimTime);
		var index = 0;
		var baseY = 98 + __instance.scroll + slideIn;

		if (__instance.runSummary.GetEnemyDiedTo() is { } enemyDiedToKey && DB.currentLocale.strings.TryGetValue($"enemy.{enemyDiedToKey}.name", out var enemyName))
		{
			Draw.Text(Instance.Localizations.Localize(["enemyDiedTo"]), 240, baseY + index * 12, color: Colors.textMain);
			Draw.Text(enemyName, 400, baseY + index * 12, color: Colors.textBold, align: TAlign.Right);
			index++;
		}
		// ReSharper restore RedundantAssignment
		// ReSharper restore UselessBinaryOperation
	}

	private static IEnumerable<CodeInstruction> RunSummaryRoute_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Ldstr("runSummary.turnsTaken"))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 1.0),
					new CodeInstruction(OpCodes.Sub)
				)
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 1.0),
					new CodeInstruction(OpCodes.Sub)
				)

				.Find(ILMatches.Ldstr("runSummary.hullDamageTaken"))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 2.0),
					new CodeInstruction(OpCodes.Sub)
				)
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 2.0),
					new CodeInstruction(OpCodes.Sub)
				)

				.Find(ILMatches.Ldstr("runSummary.seed"))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 3.0),
					new CodeInstruction(OpCodes.Sub)
				)
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 3.0),
					new CodeInstruction(OpCodes.Sub)
				)

				.Find(ILMatches.Ldstr("runSummary.ship"))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 4.0),
					new CodeInstruction(OpCodes.Sub)
				)
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 4.0),
					new CodeInstruction(OpCodes.Sub)
				)

				.Find(ILMatches.Ldstr("runSummary.difficulty"))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 5.0),
					new CodeInstruction(OpCodes.Sub)
				)
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldfld("y"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_R8, 5.0),
					new CodeInstruction(OpCodes.Sub)
				)

				.Find(ILMatches.Call(AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Render))))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler_HijackArtifactRender))))

				.Find(
					ILMatches.Ldloc<Card>(originalMethod).CreateLdlocaInstruction(out var ldlocaCard),
					ILMatches.Ldloc<CardSummary>(originalMethod).CreateLdlocInstruction(out var ldlocCardSummary),
					ILMatches.Ldfld("upgrade"),
					ILMatches.Stfld("upgrade")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocaCard,
					ldlocCardSummary,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler_ApplyExtraCardData)))
				)
				.Find(
					ILMatches.Ldloc<Card>(originalMethod).CreateLdlocInstruction(out var ldlocCard),
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
			Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void RunSummaryRoute_Render_Transpiler_HijackArtifactRender(Artifact artifact, G g, Vec restingPosition, bool showAsUnknown, bool autoFocus, bool showCount, Rect? overrideRect)
	{
		artifact.Render(g, restingPosition, showAsUnknown, autoFocus, showCount, overrideRect);

		if (LastRunSummaryRoute?.runSummary is not { } summary)
			return;

		if (summary.GetTimesArtifactsTriggered().TryGetValue(artifact.Key(), out var timesTriggered) && timesTriggered > 0)
		{
			var rect = new Rect(0, 0, 11, 11) + restingPosition;
			var box = g.Push(rect: rect, autoFocus: autoFocus);
			Draw.Text(DB.IntStringCache(timesTriggered), box.rect.x + 6, box.rect.y + 13, outline: null, color: Color.Lerp(Colors.white, Colors.black, 0.5), align: TAlign.Center, dontSubstituteLocFont: true);
			g.Pop();
		}
	}

	private static void RunSummaryRoute_Render_Transpiler_ApplyExtraCardData(ref Card card, CardSummary cardSummary)
	{
		if (LastRunSummaryRoute is not { } route)
			return;
		var fakeState = Instance.Helper.ModData.GetModData<State>(route, "FakeState");

		if (!Instance.Helper.ModData.TryGetModData<int>(cardSummary, "CardId", out var cardId))
		{
			cardId = Mutil.NextRandInt();
			Instance.Helper.ModData.SetModData(cardSummary, "CardId", cardId);
		}

		// forcing a GetActions call to make weird cards behave, like Jester's
		card.GetActions(DB.fakeState, DB.fakeCombat);

		card = Mutil.DeepCopy(card);
		card.uuid = cardId;

		card.SetTimesPlayed(fakeState, cardSummary.GetTimesPlayed() ?? 0);
		foreach (var traitOverride in cardSummary.GetTraitOverrides())
			Instance.Helper.Content.Cards.SetCardTraitOverride(fakeState, card, traitOverride.Key, traitOverride.Value, permanent: true);
	}

	private static Rect RunSummaryRoute_Render_Transpiler_HijackCardRender(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale, Card card)
	{
		if (LastRunSummaryRoute is not { } route)
			return Draw.Text(str, x, y, Instance.KokoroApi?.Assets.PinchCompactFont ?? font, color, colorForce, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
		var fakeState = Instance.Helper.ModData.GetModData<State>(route, "FakeState");

		var traitIndex = 0;
		foreach (var trait in Instance.Helper.Content.Cards.GetAllCardTraits(fakeState, card))
		{
			if (trait.Value.PermanentOverride is not { } permanentOverride)
				continue;
			if (trait.Key.Configuration.Icon(fakeState, card) is not { } icon)
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

		if (card.GetTimesPlayed(fakeState) is { } timesPlayed and > 0)
		{
			var counterColor = Color.Lerp(color ?? Colors.textMain, Colors.black, 0.5);
			str = $"{str} <c={counterColor}>({timesPlayed})</c>";
		}

		var rect = Draw.Text(str, x, y, Instance.KokoroApi?.Assets.PinchCompactFont ?? font, color, colorForce, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
		var box = MG.inst.g.Push(new UIKey(StableUK.card, card.uuid), new Rect(x - 82, y - 14 - route.scroll, rect.w + 4, rect.h + 4));

		if (box.IsHover())
			MG.inst.g.tooltips.Add(box.rect.xy.round() + new Vec(rect.w + 8), new TTCard { card = card, showCardTraitTooltips = true });

		MG.inst.g.Pop();
		return rect;
	}
}