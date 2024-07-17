using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal static class CharacterPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetDisplayName), [typeof(string), typeof(State)]),
			postfix: new HarmonyMethod(typeof(CharacterPatches), nameof(Character_GetDisplayName_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render)),
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
		if (g.metaRoute is not null)
			return;
		if (g.state.IsOutsideRun() && g.state.route is not NewRunOptions)
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
				if (!g.state.IsOutsideRun())
					g.tooltips.AddText(g.tooltips.pos, I18n.CharacterEligibleForDuoArtifactNoDuos);
				break;
			case DuoArtifactEligibity.NoDuosForThisCrew:
				if (!g.state.IsOutsideRun())
					g.tooltips.AddText(g.tooltips.pos, I18n.CharacterEligibleForDuoArtifactNoMatchingDuos);
				break;
			case DuoArtifactEligibity.Eligible:
				var decks = g.state.IsOutsideRun()
					? g.state.runConfig.selectedChars.ToHashSet()
					: g.state.characters.Select(c => c.deckType).WhereNotNull().ToHashSet();
				decks.Add(deck);

				g.tooltips.AddText(g.tooltips.pos, I18n.CharacterEligibleForDuoArtifact);
				foreach (var duoType in ModEntry.Instance.Database.GetMatchingDuoArtifactTypes(decks))
				{
					if (DB.artifacts.FirstOrNull(kvp => kvp.Value == duoType) is not { } kvp)
						continue;
					if (ModEntry.Instance.Database.GetDuoArtifactTypeOwnership(duoType) is not { } ownership)
						continue;
					if (!ownership.Contains(deck))
						continue;

					if (g.state.storyVars.artifactsOwned.Contains(kvp.Key))
						g.tooltips.AddText(g.tooltips.pos, $"<c=artifact>{Loc.T($"artifact.{kvp.Key}.name")}</c>\n{I18n.GetDuoArtifactTooltip(ownership, @long: false)}");
					else
						g.tooltips.AddText(g.tooltips.pos, $"<c=artifact>???</c>\n{I18n.GetDuoArtifactTooltip(ownership, @long: false)}");
				}

				break;
		}
	}
}
