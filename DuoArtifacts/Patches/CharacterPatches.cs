using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal static class CharacterPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetDisplayName), [typeof(string), typeof(State)]),
			postfix: new HarmonyMethod(typeof(CharacterPatches), nameof(Character_GetDisplayName_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render)),
			postfix: new HarmonyMethod(typeof(CharacterPatches), nameof(Character_Render_Postfix))
		);
	}

	private static void Character_GetDisplayName_Postfix(string charId, ref string __result)
	{
		if (charId == Instance.Database.DuoArtifactDeck.GlobalName)
			__result = I18n.DuoArtifactDeckName;
		else if (charId == Instance.Database.TrioArtifactDeck.GlobalName)
			__result = I18n.TrioArtifactDeckName;
		else if (charId == Instance.Database.ComboArtifactDeck.GlobalName)
			__result = I18n.ComboArtifactDeckName;
	}

	private static void Character_Render_Postfix(Character __instance, G g, bool mini, bool renderLocked, bool canFocus, bool showTooltips)
	{
		if (!showTooltips || !canFocus || renderLocked || __instance.deckType is not { } deck)
			return;

		var key = new UIKey(mini ? StableUK.char_mini : StableUK.character, (int)deck, __instance.type);
		if (g.boxes.FirstOrDefault(b => b.key == key) is not { } box)
			return;
		if (!box.IsHover())
			return;

		switch (ModEntry.Instance.GetDuoArtifactEligibity(deck, g.state))
		{
			case DuoArtifactEligibity.InvalidState:
			case DuoArtifactEligibity.RequirementsNotSatisfied:
				break;
			case DuoArtifactEligibity.NoDuosForThisCharacter:
				g.tooltips.AddText(g.tooltips.pos, I18n.CharacterEligibleForDuoArtifactNoDuos);
				break;
			case DuoArtifactEligibity.NoDuosForThisCrew:
				g.tooltips.AddText(g.tooltips.pos, I18n.CharacterEligibleForDuoArtifactNoMatchingDuos);
				break;
			case DuoArtifactEligibity.Eligible:
				g.tooltips.AddText(g.tooltips.pos, I18n.CharacterEligibleForDuoArtifact);
				break;
		}
	}
}
