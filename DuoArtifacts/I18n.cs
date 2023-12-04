namespace Shockah.DuoArtifacts;

internal static class I18n
{
	public static string DuoArtifactDeckName => "Duo";

	public static string FluxAltGlossaryName => "Flux";
	public static string FluxAltGlossaryDescription => "Whenever this ship attacks, it gains <c=status>TEMP SHIELD</c>. <c=downside>Decreases by 1 at end of turn.</c>";
	public static CustomTTGlossary FluxAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_libra, () => FluxAltGlossaryName, () => FluxAltGlossaryDescription);

	public static string HeatAltGlossaryName => "Heat";
	public static string HeatAltGlossaryDescription => "Excess heat. If heat is too high at end of turn, <c=action>OVERHEAT</c> <c=downside>(take 1 hull damage and reset heat to 0)</c>.";
	public static CustomTTGlossary HeatAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_heat, () => HeatAltGlossaryName, () => HeatAltGlossaryDescription);

	public static string MaxShieldLowerAltGlossaryName => "Shield capacity";
	public static string MaxShieldLowerAltGlossaryDescription => "Max shield is lowered for the rest of combat.";
	public static CustomTTGlossary MaxShieldLowerAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_maxShieldLower, () => MaxShieldLowerAltGlossaryName, () => MaxShieldLowerAltGlossaryDescription);

	public static string ScorchingGlossaryName => "Scorching";
	public static string ScorchingGlossaryDescription => "The object takes damage each turn. If this object collides with a ship, the ship gains 2 <c=status>HEAT</c>.";
	public static CustomTTGlossary ScorchingGlossary = new(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => ScorchingGlossaryName, () => ScorchingGlossaryDescription);

	public static string DroneMoveAltGlossaryName => "DRONE MOVE";
	public static string DroneMoveAltGlossaryDescription => "Instantly move all <c=midrow>midrow objects</c> in some direction.";
	public static CustomTTGlossary DroneMoveGlossary = new(CustomTTGlossary.GlossaryType.action, () => StableSpr.icons_droneMoveRandom, () => DroneMoveAltGlossaryName, () => DroneMoveAltGlossaryDescription);

	public static string WormStatusName => "WORM";
	public static string WormStatusDescription => "Cancels {0} intents at the start of the player's turn. <c=downside>Decreases by 1 at end of turn.</c>";
	public static string WormStatusAltGlossaryDescription => "Cancels intents at the start of the player's turn. <c=downside>Decreases by 1 at end of turn.</c>";
	public static CustomTTGlossary WormStatusGlossary = new(CustomTTGlossary.GlossaryType.action, () => (Spr)DrakeMaxArtifact.WormSprite.Id!.Value, () => WormStatusName, () => WormStatusAltGlossaryDescription);

	public static string BooksCatArtifactName => "Books-CAT Duo Artifact";
	public static string BooksCatArtifactTooltip => "At the end of each turn, gain <c=shard>SHARD</c> equal to unspent <c=comp>energy</c>.";

	public static string BooksDrakeArtifactName => "Books-Drake Duo Artifact";
	public static string BooksDrakeArtifactTooltip => "Whenever you play an attack, lose 2 <c=shard>SHARD</c>: the attack becomes piercing. If it was already piercing, it stuns. If it was already stunning, <c=eunice>TOTAL STUN</c> the opponent.";

	public static string BooksDizzyArtifactName => "Books-Dizzy Duo Artifact";
	public static string BooksDizzyArtifactTooltip => "<c=dizzy>SHIELD</c> and <c=shard>SHARD</c> can be used interchangeably.";

	public static string BooksMaxArtifactName => "Books-Max Duo Artifact";
	public static string BooksMaxArtifactTooltip => "Whenever you <c=hacker>EXHAUST</c> a card, gain 1 <c=shard>SHARD</c>.";

	public static string CatDizzyArtifactName => "CAT-Dizzy Duo Artifact";
	public static string CatDizzyArtifactTooltip => "The first time you would lose <c=dizzy>SHIELD</c> or hull due to damage each combat, gain <c=comp>PERFECT SHIELD</c> equal to your <c=dizzy>SHIELD</c> + 2. <c=downside>Lose ALL <c=dizzy>max shield</c>.</c>";

	public static string CatDrakeArtifactName => "CAT-Drake Duo Artifact";
	public static string CatDrakeArtifactTooltip => "Whenever you gain <c=eunice>SERENITY</c>, gain <c=comp>TIMESTOP</c>. Whenever you gain <c=comp>TIMESTOP</c>, gain <c=eunice>SERENITY</c>.";

	public static string CatIsaacArtifactName => "CAT-Isaac Duo Artifact";
	public static string CatIsaacArtifactTooltip => "Whenever you are about to <c=goat>LAUNCH</c> into an object and doing so would not benefit you, <c=goat>DRONESHIFT</c> the shortest possible distance to avoid doing that.";

	public static string CatRiggsArtifactName => "CAT-Riggs Duo Artifact";
	public static string CatRiggsArtifactTooltip => "<c=comp>DISCOUNT</c> the first extra card drawn each turn.";

	public static string CatMaxArtifactName => "CAT-Max Duo Artifact";
	public static string CatMaxArtifactTooltip => "Gain 1 <c=status>random positive status</c> each turn.";

	public static string CatPeriArtifactName => "CAT-Peri Duo Artifact";
	public static string CatPeriArtifactTooltip => "Whenever you play a <c=comp>TEMPORARY</c> attack, gain 1 <c=peri>OVERDRIVE</c>.\n<c=downside>Lose an extra <c=peri>OVERDRIVE</c> each turn.</c>";

	public static string DizzyDrakeArtifactName => "Dizzy-Drake Duo Artifact";
	public static string DizzyDrakeArtifactTooltip => "<c=eunice>OVERHEAT</c> now causes you to lose 2 <c=dizzy>(TEMP) SHIELD</c> instead of hull, if possible.";

	public static string DizzyMaxArtifactName => "Dizzy-Max Duo Artifact";
	public static string DizzyMaxArtifactTooltip => "At the start of combat, gain a <c=card>Dizzy-Max Duo Artifact Card</c>.";
	public static string DizzyMaxArtifactCardName => "Dizzy-Max Duo Artifact Card";
	public static string DizzyMaxArtifactCardDescription => "Lose 3 <c=status>(TEMP) SHIELD</c>: gain 1 <c=status>BOOST</c>.";

	public static string DizzyPeriArtifactName => "Dizzy-Peri Duo Artifact";
	public static string DizzyPeriArtifactTooltip => "Any gained <c=dizzy>SHIELD</c> over <c=dizzy>max shield</c> is converted into <c=peri>OVERDRIVE</c> instead.\n<c=downside>Lose <c=dizzy>SHIELD</c> equal to <c=peri>OVERDRIVE</c> each turn.</c>";

	public static string DrakeIsaacArtifactName => "Drake-Isaac Duo Artifact";
	public static string DrakeIsaacArtifactTooltip => "Whenever you <c=goat>LAUNCH</c> a <c=goat>midrow object</c>, lose 1 <c=eunice>HEAT</c>: the object gains <c=eunice>SCORCHING</c>.";

	public static string DrakeMaxArtifactName => "Drake-Max Duo Artifact";
	public static string DrakeMaxArtifactTooltip => "At the start of combat, shuffle a <c=card>Drake-Max Duo Artifact Card</c> and a <c=cardtrait>TEMPORARY</cardtrait> <c=card>Worm</c> into your deck.";
	public static string DrakeMaxArtifactCardName => "Drake-Max Duo Artifact Card";
	public static string DrakeMaxArtifactCardDescription => "<c=cardtrait>Exhaust</c> all <c=card>Worm</c> in your hand. Apply <c=status>WORM</c> to the enemy for each.";

	public static string DrakePeriArtifactName => "Drake-Peri Duo Artifact";
	public static string DrakePeriArtifactTooltip => "Whenever you <c=eunice>OVERHEAT</c>, convert your <c=peri>OVERDRIVE</c> into <c=peri>POWERDRIVE</c>.";

	public static string DrakeRiggsArtifactName => "Drake-Riggs Duo Artifact";
	public static string DrakeRiggsArtifactTooltip => "Once a turn, when you have no <c=riggs>EVADE</c>, you may still <c=riggs>EVADE</c>. Gain 1 <c=eunice>HEAT</c>.";

	public static string IsaacMaxArtifactName => "Isaac-Max Duo Artifact";
	public static string IsaacMaxArtifactTooltip => "Whenever you <c=hacker>DISCARD</c> or <c=hacker>EXHAUST</c> any number of cards during your turn, put a <c=goat>BUBBLE</c> on a random <c=goat>midrow object</c> without one in front of the ship. If there are none, <c=goat>LAUNCH</c> an <c=goat>asteroid</c> on a random space in front of the ship.";

	public static string IsaacPeriArtifactName => "Isaac-Peri Duo Artifact";
	public static string IsaacPeriArtifactTooltip => "Your <c=goat>Attack Drones</c> benefit from <c=peri>OVERDRIVE</c>, <c=peri>POWERDRIVE</c> and <c=peri>FLUX</c>.";

	public static string IsaacRiggsArtifactName => "Isaac-Riggs Duo Artifact";
	public static string IsaacRiggsArtifactTooltip => "<c=riggs>EVADE</c> and <c=goat>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=riggs>EVADE</c> on the first turn.";

	public static string MaxPeriArtifactName => "Max-Peri Duo Artifact";
	public static string MaxPeriArtifactTooltip => "Your leftmost <c=action>ATTACK</c> deals 1 more damage.\nYour rightmost <c=action>ATTACK</c> does an extra 1 damage shot.\nThe last <c=action>ATTACK</c> in your hand gets no bonuses.";

	public static string MaxRiggsArtifactName => "Max-Riggs Duo Artifact";
	public static string MaxRiggsArtifactTooltip => "At the start of combat, gain a <c=card>Max-Riggs Duo Artifact Card</c>.";
	public static string MaxRiggsArtifactCardName => "Max-Riggs Duo Artifact Card";
}
