using HarmonyLib;
using System;

namespace Shockah.Bloch;

internal sealed class WavyDialogueManager
{
	private static string? RenderedShoutText;
	private static bool ShouldDisplayWavyText;
	private static bool IsRenderingShoutText;

	public WavyDialogueManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Dialogue), nameof(Dialogue.Render)),
			prefix: new HarmonyMethod(GetType(), nameof(Dialogue_Render_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Dialogue_Render_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render)),
			prefix: new HarmonyMethod(GetType(), nameof(Character_Render_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Character_Render_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.Text)),
			prefix: new HarmonyMethod(GetType(), nameof(Draw_Text_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.RenderCharacter)),
			prefix: new HarmonyMethod(GetType(), nameof(Draw_RenderCharacter_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.RenderCharacterOutline)),
			prefix: new HarmonyMethod(GetType(), nameof(Draw_RenderCharacterOutline_Prefix))
		);
	}

	private static void ModifyText(ref Rect rect, ref Color color)
	{
		rect.y += Math.Sin((MG.inst.g.state.time - rect.x / 64) / Math.PI * 12);
		color.a *= 0.9 - (Math.Sin(MG.inst.g.state.time / Math.PI * 15) / 2 + 0.5) * 0.1;
	}

	private static void Dialogue_Render_Prefix(Dialogue __instance)
	{
		if (__instance.routeOverride is not null || !__instance.IsVisible() || !__instance.ctx.running || DB.story.GetNode(__instance.ctx.script) is null || __instance.shout is not { } shout)
			return;
		RenderedShoutText = shout.GetText();
		ShouldDisplayWavyText = shout.who == ModEntry.Instance.BlochDeck.UniqueName && shout.loopTag != "talking";
	}

	private static void Dialogue_Render_Finalizer()
	{
		RenderedShoutText = null;
		ShouldDisplayWavyText = false;
		IsRenderingShoutText = false;
	}

	private static void Character_Render_Prefix(Character __instance, G g, bool showDialogue)
	{
		if (!showDialogue || g.state.ship.hull <= 0 || __instance.shout is not { delay: 0 } shout)
			return;
		RenderedShoutText = shout.GetText();
		ShouldDisplayWavyText = __instance.deckType == ModEntry.Instance.BlochDeck.Deck;
	}

	private static void Character_Render_Finalizer(Character __instance, G g, bool showDialogue)
	{
		if (!showDialogue || g.state.ship.hull <= 0 || __instance.shout is not { delay: 0 })
			return;

		RenderedShoutText = null;
		ShouldDisplayWavyText = false;
		IsRenderingShoutText = false;
	}

	private static void Draw_Text_Prefix(string str, bool dontDraw)
		=> IsRenderingShoutText = !dontDraw && str == RenderedShoutText;

	private static void Draw_RenderCharacter_Prefix(ref Rect dst, ref Color color)
	{
		if (!ShouldDisplayWavyText || !IsRenderingShoutText)
			return;
		ModifyText(ref dst, ref color);
	}

	private static void Draw_RenderCharacterOutline_Prefix(ref Rect dst, ref Color color)
	{
		if (!ShouldDisplayWavyText || !IsRenderingShoutText)
			return;
		ModifyText(ref dst, ref color);
	}
}