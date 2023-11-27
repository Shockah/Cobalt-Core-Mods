namespace Shockah.DuoArtifacts;

internal static class I18n
{
	public static string DuoArtifactDeckName => "Duo";

	public static string FluxAltGlossaryName => "Flux";
	public static string FluxAltGlossaryDescription => "Whenever this ship attacks, it gains <c=status>TEMP SHIELD</c>. <c=downside>Decreases by 1 at end of turn.</c>";

	public static string BooksDrakeArtifactName => "Books-Drake Duo Artifact";
	public static string BooksDrakeArtifactTooltip => "Whenever you play an attack, lose 2 <c=shard>SHARD</c>: the attack becomes piercing. If it was already piercing, it stuns. If it was already stunning, <c=eunice>TOTAL STUN</c> the opponent.";

	public static string DizzyDrakeArtifactName => "Dizzy-Drake Duo Artifact";
	public static string DizzyDrakeArtifactTooltip => "<c=eunice>OVERHEAT</c> now causes you to lose 2 <c=dizzy>(TEMP) SHIELD</c> instead of hull, if possible.";

	public static string DrakePeriArtifactName => "Drake-Peri Duo Artifact";
	public static string DrakePeriArtifactTooltip => "Whenever you <c=eunice>OVERHEAT</c>, convert your <c=peri>OVERDRIVE</c> into <c=peri>POWERDRIVE</c>.";

	public static string IsaacPeriArtifactName => "Isaac-Peri Duo Artifact";
	public static string IsaacPeriArtifactTooltip => "Your <c=goat>Attack Drones</c> benefit from <c=peri>OVERDRIVE</c>, <c=peri>POWERDRIVE</c> and <c=peri>FLUX</c>.";

	public static string IsaacRiggsArtifactName => "Isaac-Riggs Duo Artifact";
	public static string IsaacRiggsArtifactTooltip => "<c=riggs>EVADE</c> and <c=goat>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=riggs>EVADE</c> on the first turn.";
}
