using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.CrewSelectionHelper;

internal static class NewRunOptionsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private const int CharactersPerRow = 2;
	private const int MaxCharactersOnScreen = 8;

	private static readonly Lazy<Func<Rect>> CharSelectPosGetter = new(() => AccessTools.DeclaredField(typeof(NewRunOptions), "charSelectPos").EmitStaticGetter<Rect>());

	private static int ScrollPosition = 0;

	private static int MaxScroll
	{
		get
		{
			int totalRows = (int)Math.Ceiling((double)NewRunOptions.allChars.Count / CharactersPerRow);
			int maxScroll = Math.Max(0, totalRows * CharactersPerRow - MaxCharactersOnScreen);
			return maxScroll;
		}
	}

	private static int MaxPageByPageScroll
	{
		get
		{
			int totalPages = (int)Math.Ceiling((double)NewRunOptions.allChars.Count / MaxCharactersOnScreen);
			int maxScroll = Math.Max(0, (totalPages - 1) * MaxCharactersOnScreen);
			return maxScroll;
		}
	}

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter)),
			postfix: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_OnEnter_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render)),
			prefix: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_Render_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), "CharSelect"),
			postfix: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_CharSelect_Postfix)),
			transpiler: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_CharSelect_Transpiler))
		);
	}

	private static void NewRunOptions_OnEnter_Postfix()
		=> ScrollPosition = 0;

	private static void NewRunOptions_Render_Prefix()
	{
		int mouseScroll = (int)Math.Round(-Input.scrollY / 120);
		if (mouseScroll != 0)
			ScrollPosition = Math.Clamp(ScrollPosition + CharactersPerRow * mouseScroll, 0, MaxScroll);
	}

	private static void NewRunOptions_CharSelect_Postfix(G g)
	{
		var charSelectPos = CharSelectPosGetter.Value();

		if (ScrollPosition > 0)
		{
			Rect rect = new(charSelectPos.x + 18, charSelectPos.y - 52, 33, 24);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Max(0, ScrollPosition - MaxCharactersOnScreen));
			RotatedButtonSprite(g, rect, StableUK.btn_move_left, StableSpr.buttons_move, StableSpr.buttons_move_on, null, null, inactive: false, flipX: true, flipY: false, onMouseDown, autoFocus: false, noHover: false, gamepadUntargetable: true);
		}

		if (ScrollPosition < MaxScroll)
		{
			Rect rect = new(charSelectPos.x + 18, charSelectPos.y + 140, 33, 24);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Clamp(ScrollPosition + MaxCharactersOnScreen, 0, MaxPageByPageScroll));
			RotatedButtonSprite(g, rect, StableUK.btn_move_right, StableSpr.buttons_move, StableSpr.buttons_move_on, null, null, inactive: false, flipX: false, flipY: false, onMouseDown, autoFocus: false, noHover: false, gamepadUntargetable: true);
		}
	}

	private static IEnumerable<CodeInstruction> NewRunOptions_CharSelect_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)

				.Find(ILMatches.Ldstr("newRunOptions.crew"))
				.Find(ILMatches.Call("Text"))
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_CharSelect_Transpiler_HijackDrawCrewText)))
				)

				.Find(ILMatches.Ldstr("newRunOptions.crewCount"))
				.Find(ILMatches.Call("Text"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_CharSelect_Transpiler_HijackDrawCrewCountText))))

				.Find(ILMatches.Ldsfld("allChars"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_CharSelect_Transpiler_ModifyAllChars)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Hijacking a real method call")]
	private static Rect NewRunOptions_CharSelect_Transpiler_HijackDrawCrewText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale, RunConfig runConfig)
	{
		var orderedSelectedChars = runConfig.selectedChars.OrderBy(NewRunOptions.allChars.IndexOf).ToList();
		for (int i = 0; i < 3; i++)
		{
			Deck? deck = i < orderedSelectedChars.Count ? orderedSelectedChars[i] : null;
			string charText = deck is null ? I18n.EmptyCrewSlot : Loc.T($"char.{deck.Value.Key()}");
			Color charTextColor = deck is null || !DB.decks.TryGetValue(deck.Value, out var deckDef) ? Colors.downside.fadeAlpha(0.4) : deckDef.color;
			Draw.Text(charText, x, y - 5 + i * 8, font, charTextColor);
		}
		return new();
	}

	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Hijacking a real method call")]
	private static Rect NewRunOptions_CharSelect_Transpiler_HijackDrawCrewCountText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale)
	{
		// do nothing
		return new();
	}

	private static List<Deck> NewRunOptions_CharSelect_Transpiler_ModifyAllChars(List<Deck> allChars)
		=> allChars.Skip(ScrollPosition).Take(MaxCharactersOnScreen).ToList();

	// mostly copy-paste of SharedArt.ButtonResult, without too many improvements
	private static SharedArt.ButtonResult RotatedButtonSprite(G g, Rect rect, UIKey key, Spr sprite, Spr spriteHover, Spr? spriteDown = null, Color? boxColor = null, bool inactive = false, bool flipX = false, bool flipY = false, OnMouseDown? onMouseDown = null, bool autoFocus = false, bool noHover = false, bool showAsPressed = false, bool gamepadUntargetable = false, UIKey? leftHint = null, UIKey? rightHint = null)
	{
		bool gamepadUntargetable2 = gamepadUntargetable;
		Box box = g.Push(key, rect, null, autoFocus, inactive, gamepadUntargetable2, ReticleMode.Quad, onMouseDown, null, null, null, 0, rightHint, leftHint);
		Vec xy = box.rect.xy;
		bool flag = !noHover && (box.IsHover() || showAsPressed) && !inactive;
		if (spriteDown.HasValue && box.IsHover() && Input.mouseLeft)
			showAsPressed = true;
		double rotation = Math.PI / 2;
		Draw.Sprite((!showAsPressed) ? (flag ? spriteHover : sprite) : (spriteDown ?? spriteHover), xy.x + Math.Sin(rotation) * rect.w, xy.y - Math.Cos(rotation) * rect.h, flipX, flipY, rotation, null, null, null, null, boxColor);
		SharedArt.ButtonResult buttonResult = default;
		buttonResult.isHover = flag;
		buttonResult.FIXME_isHoverForTooltip = !noHover && box.IsHover();
		buttonResult.v = xy;
		buttonResult.innerOffset = new Vec(0.0, showAsPressed ? 2 : (flag ? 1 : 0));
		SharedArt.ButtonResult result = buttonResult;
		g.Pop();
		return result;
	}
}
