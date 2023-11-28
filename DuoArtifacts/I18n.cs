using System;

namespace Shockah.DuoArtifacts;

internal static class I18n
{
	public static string DuoArtifactDeckName => "Duo";

	public static string FluxAltGlossaryName => "Flux";
	public static string FluxAltGlossaryDescription => "Whenever this ship attacks, it gains <c=status>TEMP SHIELD</c>. <c=downside>Decreases by 1 at end of turn.</c>";
	public static CustomTTGlossary FluxAltGlossary = new(CustomTTGlossary.GlossaryType.status, Enum.Parse<Spr>("icons_libra"), FluxAltGlossaryName, FluxAltGlossaryDescription);

	public static string HeatAltGlossaryName => "Heat";
	public static string HeatAltGlossaryDescription => "Excess heat. If heat is too high at end of turn, <c=action>OVERHEAT</c> <c=downside>(take 1 hull damage and reset heat to 0)</c>.";
	public static CustomTTGlossary HeatAltGlossary = new(CustomTTGlossary.GlossaryType.status, Enum.Parse<Spr>("icons_heat"), HeatAltGlossaryName, HeatAltGlossaryDescription);

	public static string AutododgeAltGlossaryName => "Autododge";
	public static string AutododgeAltGlossaryDescription => "If fired on, will completely move out of the way to the side. <c=downside>Decreases by 1 every time it triggers. Goes away at start of next turn.</c>";
	public static CustomTTGlossary AutododgeAltGlossary = new(CustomTTGlossary.GlossaryType.status, Enum.Parse<Spr>("icons_autododgeRight"), AutododgeAltGlossaryName, AutododgeAltGlossaryDescription);

	public static string BooksDrakeArtifactName => "Books-Drake Duo Artifact";
	public static string BooksDrakeArtifactTooltip => "Whenever you play an attack, lose 2 <c=shard>SHARD</c>: the attack becomes piercing. If it was already piercing, it stuns. If it was already stunning, <c=eunice>TOTAL STUN</c> the opponent.";

	public static string BooksDizzyArtifactName => "Books-Dizzy Duo Artifact";
	public static string BooksDizzyArtifactTooltip => "<c=dizzy>SHIELD</c> and <c=shard>SHARD</c> can be used interchangeably.";

	public static string DizzyDrakeArtifactName => "Dizzy-Drake Duo Artifact";
	public static string DizzyDrakeArtifactTooltip => "<c=eunice>OVERHEAT</c> now causes you to lose 2 <c=dizzy>(TEMP) SHIELD</c> instead of hull, if possible.";

	public static string DrakePeriArtifactName => "Drake-Peri Duo Artifact";
	public static string DrakePeriArtifactTooltip => "Whenever you <c=eunice>OVERHEAT</c>, convert your <c=peri>OVERDRIVE</c> into <c=peri>POWERDRIVE</c>.";

	public static string DrakeRiggsArtifactName => "Drake-Riggs Duo Artifact";
	public static string DrakeRiggsArtifactTooltip => "Once a turn, when you have no <c=riggs>EVADE</c>, you may still <c=riggs>EVADE</c>. Gain 1 <c=eunice>HEAT</c>.";

	public static string IsaacPeriArtifactName => "Isaac-Peri Duo Artifact";
	public static string IsaacPeriArtifactTooltip => "Your <c=goat>Attack Drones</c> benefit from <c=peri>OVERDRIVE</c>, <c=peri>POWERDRIVE</c> and <c=peri>FLUX</c>.";

	public static string IsaacRiggsArtifactName => "Isaac-Riggs Duo Artifact";
	public static string IsaacRiggsArtifactTooltip => "<c=riggs>EVADE</c> and <c=goat>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=riggs>EVADE</c> on the first turn.";

	public static string MaxRiggsArtifactName => "Max-Riggs Duo Artifact";
	public static string MaxRiggsArtifactTooltip => "After using 2 <c=riggs>EVADE</c> in a row in the same direction, gain 1 <c=hacker>AUTODODGE</c>.";
	public static string MaxRiggsArtifactTooltipLeft => "You last evaded left.";
	public static string MaxRiggsArtifactTooltipRight => "You last evaded right.";
}
