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
using Newtonsoft.Json.Converters;
using Nickel;

namespace Shockah.CodexHelper;

internal sealed partial class ProfileSettings
{
	[JsonProperty] public bool TrackCardTakenCompletion = true;
	[JsonProperty] public bool TrackCardSeenCompletion = true;
	[JsonProperty] public CardProgressDisplayStyleWhilePickingEnum CardProgressDisplayStyleWhilePicking = CardProgressDisplayStyleWhilePickingEnum.Icon;

	[JsonConverter(typeof(StringEnumConverter))]
	public enum CardProgressDisplayStyleWhilePickingEnum
	{
		Text, Icon, Both
	}
}

internal static class StateVarsCardExt
{
	public static bool IsCardSeen(this StoryVars storyVars, string key, CardReward? route, bool isTaken = false)
	{
		if (isTaken)
			return true;
		
		var newlySeenCards = route is null ? null : ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(route, "NewlySeenCards");
		if (newlySeenCards is not null && newlySeenCards.Contains(key))
			return false;
		
		var seenCards = ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(storyVars, "SeenCards");
		return seenCards.Contains(key);
	}

	public static void SetCardSeen(this StoryVars storyVars, string key, CardReward? route, bool isSeen = true)
	{
		var newlySeenCards = route is null ? null : ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(route, "NewlySeenCards");
		var seenCards = ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(storyVars, "SeenCards");

		if (isSeen)
		{
			if (!seenCards.Contains(key) && newlySeenCards is not null)
				newlySeenCards.Add(key);
			seenCards.Add(key);
		}
		else
		{
			seenCards.Remove(key);
		}
	}
}

internal sealed class CardCodexProgress : IRegisterable
{
	private static ISpriteEntry NewIcon = null!;
	private static ISpriteEntry SeenIcon = null!;
	private static ISpriteEntry TakenIcon = null!;
	private static ISpriteEntry SeenDot = null!;
	
	private static CardReward? RenderedCardReward;
	private static CardBrowse? RenderedCardBrowse;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		NewIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardNew.png"));
		SeenIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardSeen.png"));
		TakenIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTaken.png"));
		SeenDot = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardSeenDot.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_Render_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix_Last)), priority: Priority.Last),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.IsDiscovered)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_IsDiscovered_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(LogBook), nameof(LogBook.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LogBook_Render_Transpiler))
		);

		if (ModEntry.Instance.Helper.ModRegistry.LoadedMods.ContainsKey("Nickel.Essentials"))
			ModEntry.Instance.Harmony.Patch(
				original: AccessTools.AllAssemblies().First(a => a.GetName().Name == "Nickel.Essentials").GetType("Nickel.Essentials.LogbookReplacement").InnerTypes().SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<LogBook_Render_Prefix>g__RenderSeenCards") && m.ReturnType == typeof(int)),
				transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Nickel_Essentials_LogbookReplacement_LogBook_Render_Prefix_RenderSeenCards_Transpiler))
			);
	}

	private static void CardReward_Render_Prefix(CardReward __instance, G g)
	{
		if (__instance.ugpradePreview is not null)
			return;
		
		RenderedCardReward = __instance;

		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "MarkedCardsAsSeen"))
			return;
		
		foreach (var card in __instance.cards)
			g.state.storyVars.SetCardSeen(card.Key(), __instance);
		ModEntry.Instance.Helper.ModData.SetModData(__instance, "MarkedCardsAsSeen", true);
	}

	private static void CardReward_Render_Finalizer()
		=> RenderedCardReward = null;

	private static void CardBrowse_Render_Prefix(CardBrowse __instance)
	{
		if (__instance.browseSource != CardBrowse.Source.Codex)
			return;
		RenderedCardBrowse = __instance;
	}

	private static void CardBrowse_Render_Finalizer()
		=> RenderedCardBrowse = null;

	private static void Card_Render_Postfix_Last(Card __instance, G g, Vec? posOverride, UIKey? keyOverride)
	{
		if (RenderedCardReward is { } cardRewardRoute)
		{
			if (ModEntry.Instance.Settings.ProfileBased.Current.CardProgressDisplayStyleWhilePicking == ProfileSettings.CardProgressDisplayStyleWhilePickingEnum.Text)
				return;
			
			switch (ModEntry.Instance.Api.GetCardProgress(g.state, __instance.Key(), cardRewardRoute))
			{
				case ICodexHelperApi.ICardProgress.NotSeen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion || ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
					RenderIcon(ICodexHelperApi.ICardProgress.NotSeen);
					return;
				case ICodexHelperApi.ICardProgress.Seen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
					RenderIcon(ICodexHelperApi.ICardProgress.Seen);
					return;
				case ICodexHelperApi.ICardProgress.Seen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion:
					RenderIcon(ICodexHelperApi.ICardProgress.Seen, NewIcon);
					return;
				case ICodexHelperApi.ICardProgress.Taken:
				default:
					break;
			}
		}
		else if (RenderedCardBrowse is not null)
		{
			var progress = ModEntry.Instance.Api.GetCardProgress(g.state, __instance.Key());
			if (progress == ICodexHelperApi.ICardProgress.Seen && ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion)
				RenderIcon(ICodexHelperApi.ICardProgress.Seen);
		}

		void RenderIcon(ICodexHelperApi.ICardProgress progress, ISpriteEntry? iconOverride = null)
		{
			var icon = iconOverride ?? progress switch
			{
				ICodexHelperApi.ICardProgress.NotSeen => NewIcon,
				ICodexHelperApi.ICardProgress.Seen => SeenIcon,
				ICodexHelperApi.ICardProgress.Taken => TakenIcon,
				_ => throw new ArgumentOutOfRangeException(nameof(progress), progress, null)
			};
			var texture = SpriteLoader.Get(icon.Sprite)!;
			
			var position = posOverride ?? __instance.pos;
			position += new Vec(0.0, __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim));
			position = position.round();

			Draw.Sprite(icon.Sprite, position.x + 28 - texture.Width / 2, position.y + 75);

			var uiKey = keyOverride ?? __instance.UIKey();
			if (g.boxes.FirstOrDefault(b => b.key == uiKey) is not { } box)
				return;
			if (!box.IsHover())
				return;
			
			g.tooltips.tooltips.Insert(0, new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(CardCodexProgress)}")
			{
				Icon = icon.Sprite,
				TitleColor = Colors.textChoice,
				Title = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", progress.ToString(), "title"]),
				Description = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", progress.ToString(), "description"]),
			});
		}
	}
	
	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static string Card_Render_Transpiler_ModifyRarityTextIfNeeded(string rarityText, Card card, G g)
	{
		if (RenderedCardReward is not { } cardRewardRoute)
			return rarityText;
		if (ModEntry.Instance.Settings.ProfileBased.Current.CardProgressDisplayStyleWhilePicking == ProfileSettings.CardProgressDisplayStyleWhilePickingEnum.Icon)
			return rarityText;
		
		switch (ModEntry.Instance.Api.GetCardProgress(g.state, card.Key(), cardRewardRoute))
		{
			case ICodexHelperApi.ICardProgress.NotSeen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion || ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
				return GetModifiedText(ICodexHelperApi.ICardProgress.NotSeen);
			case ICodexHelperApi.ICardProgress.Seen when ModEntry.Instance.Settings.ProfileBased.Current.TrackCardTakenCompletion || ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion:
				return GetModifiedText(ICodexHelperApi.ICardProgress.Seen);
			case ICodexHelperApi.ICardProgress.Taken:
			default:
				return rarityText;
		}

		string GetModifiedText(ICodexHelperApi.ICardProgress progress)
		{
			var extra = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", progress.ToString(), "cardSubtitle"]);
			return progress switch
			{
				ICodexHelperApi.ICardProgress.NotSeen => $"{rarityText}\n<c=textBold>{extra}</c>",
				ICodexHelperApi.ICardProgress.Seen => $"{rarityText}\n<c=textMain>{extra}</c>",
				ICodexHelperApi.ICardProgress.Taken => $"{rarityText}\n<c=textFaint>{extra}</c>",
				_ => throw new ArgumentOutOfRangeException(nameof(progress), progress, null)
			};
		}
	}

	private static void Card_IsDiscovered_Postfix(Card __instance, State state, ref bool __result)
	{
		if (__result)
			return;
		if (RenderedCardBrowse is null)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion)
			return;
		if (!state.persistentStoryVars.IsCardSeen(__instance.Key(), null))
			return;

		__result = true;
	}
	
	private static IEnumerable<CodeInstruction> LogBook_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldfld("cardsOwned"),
					ILMatches.Ldloca<KeyValuePair<string, CardMeta>>(originalMethod).CreateLdlocInstruction(out var ldlocKvp),
					ILMatches.Call("get_Key"),
					ILMatches.Call("Contains"),
					ILMatches.Stloc<bool>(originalMethod).CreateLdlocaInstruction(out var ldlocaHasIt),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocKvp,
					ldlocaHasIt,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LogBook_Render_Transpiler_ModifyHasIt))),
				])
				.Find([
					ILMatches.Call("Push"),
					ILMatches.Stloc<Box>(originalMethod).CreateLdlocInstruction(out var ldlocBox),
				])
				.Find([
					ILMatches.Ldarg(1).ExtractLabels(out var labels),
					ILMatches.Call("Pop"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					ldlocKvp,
					ldlocBox,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LogBook_Render_Transpiler_RenderSeen))),
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

	private static void LogBook_Render_Transpiler_ModifyHasIt(G g, KeyValuePair<string, CardMeta> kvp, ref bool hasIt)
	{
		if (hasIt)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion)
			return;
		if (!g.state.persistentStoryVars.IsCardSeen(kvp.Key, null))
			return;
		hasIt = true;
	}

	private static void LogBook_Render_Transpiler_RenderSeen(G g, KeyValuePair<string, CardMeta> kvp, Box box)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion)
			return;

		if (g.state.persistentStoryVars.cardsOwned.Contains(kvp.Key))
			return;
		if (!g.state.persistentStoryVars.IsCardSeen(kvp.Key, null))
			return;
		
		Draw.Sprite(SeenDot.Sprite, box.rect.x + 1, box.rect.y + 1);
			
		if (!box.IsHover())
			return;
			
		g.tooltips.tooltips.Add(new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(CardCodexProgress)}")
		{
			Icon = SeenIcon.Sprite,
			TitleColor = Colors.textChoice,
			Title = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", "Seen", "title"]),
			Description = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", "Seen", "description"]),
		});
	}

	private static IEnumerable<CodeInstruction> Nickel_Essentials_LogbookReplacement_LogBook_Render_Prefix_RenderSeenCards_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("g").CreateLdfldInstruction(out var ldfldG),
				])
				.Find([
					ILMatches.AnyLdloc.CreateLdlocInstruction(out var ldlocLocals),
					ILMatches.Ldfld("card").CreateLdfldInstruction(out var ldfldCard),
				])
				.Find(ILMatches.Stfld("hasIt"))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					ldfldG,
					ldlocLocals,
					ldfldCard,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Nickel_Essentials_LogbookReplacement_LogBook_Render_Prefix_RenderSeenCards_Transpiler_ModifyHasIt))),
				])
				.Find([
					ILMatches.Ldloc<Box>(originalMethod).CreateLdlocInstruction(out var ldlocBox),
					ILMatches.Call("IsHover"),
				])
				.Find([
					ILMatches.Ldarg(0).ExtractLabels(out var labels),
					ILMatches.Ldfld("g"),
					ILMatches.Call("Pop"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					ldfldG,
					ldlocLocals,
					ldfldCard,
					ldlocBox,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Nickel_Essentials_LogbookReplacement_LogBook_Render_Prefix_RenderSeenCards_Transpiler_RenderSeen))),
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

	private static bool Nickel_Essentials_LogbookReplacement_LogBook_Render_Prefix_RenderSeenCards_Transpiler_ModifyHasIt(bool hasIt, G g, Card card)
	{
		if (hasIt)
			return true;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion)
			return false;
		if (!g.state.persistentStoryVars.IsCardSeen(card.Key(), null))
			return false;
		return true;
	}

	private static void Nickel_Essentials_LogbookReplacement_LogBook_Render_Prefix_RenderSeenCards_Transpiler_RenderSeen(G g, Card card, Box box)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.TrackCardSeenCompletion)
			return;

		var key = card.Key();

		if (g.state.persistentStoryVars.cardsOwned.Contains(key))
			return;
		if (!g.state.persistentStoryVars.IsCardSeen(card.Key(), null))
			return;
		
		Draw.Sprite(SeenDot.Sprite, box.rect.x + 1, box.rect.y + 1);
			
		if (!box.IsHover())
			return;
			
		g.tooltips.tooltips.Insert(0, new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(CardCodexProgress)}")
		{
			Icon = SeenIcon.Sprite,
			TitleColor = Colors.textChoice,
			Title = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", "Seen", "title"]),
			Description = ModEntry.Instance.Localizations.Localize(["cardCodexProgress", "Seen", "description"]),
		});
	}
}

// TODO: move to Shrike
file static class ShrikeExt
{
	public static ElementMatch<CodeInstruction> CreateLdfldInstruction(this ElementMatch<CodeInstruction> self, out ObjectRef<CodeInstruction> instructionReference)
	{
		var reference = new ObjectRef<CodeInstruction>(null!);
		instructionReference = reference;
		return self.WithDelegate((matcher, index, _) =>
		{
			matcher.MakePointerMatcher(index).CreateLdfldInstruction(out var instruction);
			reference.Value = instruction;
			return matcher;
		});
	}

	// ReSharper disable once UnusedMethodReturnValue.Local
	private static SequencePointerMatcher<CodeInstruction> TryCreateLdfldInstruction(this SequencePointerMatcher<CodeInstruction> self, out CodeInstruction? instruction)
	{
		instruction = null;
		if (self.Element().operand is FieldInfo field)
			instruction = new CodeInstruction(OpCodes.Ldfld, field);
		return self;
	}

	// ReSharper disable once UnusedMethodReturnValue.Local
	private static SequencePointerMatcher<CodeInstruction> CreateLdfldInstruction(this SequencePointerMatcher<CodeInstruction> self, out CodeInstruction instruction)
	{
		self.TryCreateLdfldInstruction(out var tryInstruction);
		instruction = tryInstruction ?? throw new SequenceMatcherException($"{self.Element()} is not a field instruction.");
		return self;
	}
}