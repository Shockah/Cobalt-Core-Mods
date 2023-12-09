namespace Shockah.Kokoro;

internal static class I18n
{
	public static string ScorchingGlossaryName => "Scorching";
	public static string ScorchingGlossaryDescription => "The object takes damage each turn. If this object is destroyed by a ship's <c=action>ATTACK</c> or <c=action>LAUNCH</c>, the ship gains {0} <c=status>HEAT</c>.";
	public static string ScorchingGlossaryAltDescription => "The object takes damage each turn. If this object is destroyed by a ship's <c=action>ATTACK</c> or <c=action>LAUNCH</c>, the ship gains <c=status>HEAT</c>.";

	public static string WormStatusName => "WORM";
	public static string WormStatusDescription => "Cancels {0} intents at the start of the player's turn. <c=downside>Decreases by 1 at end of turn.</c>";
	public static string WormStatusAltGlossaryDescription => "Cancels intents at the start of the player's turn. <c=downside>Decreases by 1 at end of turn.</c>";

	//public static string OxidationStatusName => "OXIDATION";
	//public static string OxidationStatusDescription => "If oxidation is 7 or more at end of turn, gain 1 <c=status>CORRODE</c> and set oxidation to 0.";
	//public static CustomTTGlossary OxidationStatusGlossary = new(CustomTTGlossary.GlossaryType.status, () => (Spr)DizzyIsaacArtifact.OxidationSprite.Id!.Value, () => OxidationStatusName, () => OxidationStatusDescription);
}