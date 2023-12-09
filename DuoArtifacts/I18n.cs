using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal static class I18n
{
	public static string DuoArtifactDeckName => "Duo";
	public static string DuoArtifactTooltip => "{0}-{1} Duo Artifact.";

	public static string TrioArtifactDeckName => "Duo";
	public static string TrioArtifactTooltip => "{0}-{1}-{2} Trio Artifact.";

	public static string ComboArtifactDeckName => "Combo";
	public static string ComboArtifactTooltip => "{0} Combo Artifact.";
	public static string ComboArtifactTooltipSeparator => "-";

	public static string CharacterEligibleForDuoArtifact => "Eligible for duo artifacts.";

	// can't use Loc.T, we're doing this too early
	private static string GetCharacterName(Deck character)
		=> character switch
		{
			Deck.colorless => "CAT",
			Deck.dizzy => "Dizzy",
			Deck.riggs => "Riggs",
			Deck.peri => "Peri",
			Deck.goat => "Isaac",
			Deck.eunice => "Drake",
			Deck.hacker => "Max",
			Deck.shard => "Books",
			_ => throw new ArgumentException()
		};

	public static string GetDuoArtifactTooltip(IEnumerable<Deck> characters)
	{
		var characterNamesWithColor = characters
			.Distinct()
			.Select(c => c == Deck.catartifact ? Deck.colorless : c)
			.OrderBy(NewRunOptions.allChars.IndexOf)
			.Select(c => $"<c={c.Key()}>{GetCharacterName(c)}</c>")
			.ToList();

		return characterNamesWithColor.Count switch
		{
			2 => string.Format(DuoArtifactTooltip, characterNamesWithColor[0], characterNamesWithColor[1]),
			3 => string.Format(TrioArtifactTooltip, characterNamesWithColor[0], characterNamesWithColor[1], characterNamesWithColor[2]),
			_ => string.Format(ComboArtifactTooltip, string.Join(ComboArtifactTooltipSeparator, characterNamesWithColor))
		};
	}

	public static string FluxAltGlossaryName => "Flux";
	public static string FluxAltGlossaryDescription => "Whenever this ship attacks, it gains <c=status>TEMP SHIELD</c>. <c=downside>Decreases by 1 at end of turn.</c>";
	public static CustomTTGlossary FluxAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_libra, () => FluxAltGlossaryName, () => FluxAltGlossaryDescription);

	public static string HeatAltGlossaryName => "Heat";
	public static string HeatAltGlossaryDescription => "Excess heat. If heat is too high at end of turn, <c=action>OVERHEAT</c> <c=downside>(take 1 hull damage and reset heat to 0)</c>.";
	public static CustomTTGlossary HeatAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_heat, () => HeatAltGlossaryName, () => HeatAltGlossaryDescription);

	public static string MaxShieldLowerAltGlossaryName => "Shield capacity";
	public static string MaxShieldLowerAltGlossaryDescription => "Max shield is lowered for the rest of combat.";
	public static CustomTTGlossary MaxShieldLowerAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_maxShieldLower, () => MaxShieldLowerAltGlossaryName, () => MaxShieldLowerAltGlossaryDescription);

	public static string OxidationStatusName => "OXIDATION";
	public static string OxidationStatusDescription => "If oxidation is 7 or more at end of turn, gain 1 <c=status>CORRODE</c> and set oxidation to 0.";
	public static CustomTTGlossary OxidationStatusGlossary = new(CustomTTGlossary.GlossaryType.status, () => (Spr)DizzyIsaacArtifact.OxidationSprite.Id!.Value, () => OxidationStatusName, () => OxidationStatusDescription);

	public static string BooksCatArtifactName => "Shard Synthesizer";
	public static string BooksCatArtifactTooltip => "At the end of each turn, gain <c=status>SHARD</c> equal to unspent <c=keyword>energy</c>.";

	public static string BooksDrakeArtifactName => "Stunflare Catalyst";
	public static string BooksDrakeArtifactTooltip => "Whenever you play an <c=action>ATTACK</c>, lose 2 <c=status>SHARD</c>: the <c=action>ATTACK</c> becomes piercing. If it was already piercing, it stuns. If it was already stunning, <c=action>TOTAL STUN</c> the opponent.";

	public static string BooksDizzyArtifactName => "Aegis Transmuter";
	public static string BooksDizzyArtifactTooltip => "<c=status>SHIELD</c> and <c=status>SHARD</c> can be used interchangeably.";

	public static string BooksIsaacArtifactName => "Drone Amplification Matrix";
	public static string BooksIsaacArtifactTooltip => "At the end of each turn, lose 2 <c=status>SHARD</c>: your <c=midrow>Attack Drones</c> deal 1 more damage.";

	public static string BooksMaxArtifactName => "Evaporator";
	public static string BooksMaxArtifactTooltip => "Whenever you <c=cardtrait>EXHAUST</c> a card, gain 1 <c=status>SHARD</c>.";

	public static string BooksPeriArtifactName => "Shardblade Infuser";
	public static string BooksPeriArtifactTooltip => "Whenever you play an <c=action>ATTACK</c>, lose 1 <c=status>SHARD</c>: the <c=action>ATTACK</c> deals 1 more damage.";

	public static string BooksRiggsArtifactName => "Fleetfoot Resonator";
	public static string BooksRiggsArtifactTooltip => "Gain 1 <c=status>HERMES BOOTS</c> for each 3 <c=status>SHARD</c> you have each turn.";

	public static string CatDizzyArtifactName => "Quantum Sanctuary";
	public static string CatDizzyArtifactTooltip => "The first time you would lose <c=status>SHIELD</c> or hull due to damage each combat, gain <c=status>PERFECT SHIELD</c> equal to your <c=status>SHIELD</c>. <c=downside>Lose ALL <c=status>max shield</c>.</c>";

	public static string CatDrakeArtifactName => "Temporal Heat Dissipator";
	public static string CatDrakeArtifactTooltip => "Whenever you gain <c=status>SERENITY</c>, gain <c=status>TIMESTOP</c>. Whenever you gain <c=status>TIMESTOP</c>, gain <c=status>SERENITY</c>.";

	public static string CatIsaacArtifactName => "Smart Launch System";
	public static string CatIsaacArtifactTooltip => "Twice per turn, whenever you are about to <c=action>LAUNCH</c> into an object and doing so would not benefit you, shove objects to the side to make space for it instead.";

	public static string CatMaxArtifactName => "Gashapon.EXE";
	public static string CatMaxArtifactTooltip => "Gain 1 random positive status each turn.";

	public static string CatPeriArtifactName => "Combat Training Simulator";
	public static string CatPeriArtifactTooltip => "Whenever you play a <c=cardtrait>temporary</c> <c=action>ATTACK</c>, gain 1 <c=status>OVERDRIVE</c>.\n<c=downside>Lose an extra <c=status>OVERDRIVE</c> each turn.</c>";

	public static string CatRiggsArtifactName => "Tech-Support Protocol";
	public static string CatRiggsArtifactTooltip => "<c=cardtrait>DISCOUNT</c> the first non-zero-cost extra card drawn each turn.";

	public static string DizzyDrakeArtifactName => "Frozen Control Rods";
	public static string DizzyDrakeArtifactTooltip => "<c=action>OVERHEAT</c> now causes you to lose 2 <c=status>(TEMP) SHIELD</c> instead of hull, if possible.";

	public static string DizzyIsaacArtifactName => "Corrosive Payload";
	public static string DizzyIsaacArtifactTooltip => "Whenever a <c=midrow>midrow object</c> gets destroyed by an <c=action>ATTACK</c> or <c=action>LAUNCH</c>, the ship that caused it gains 1 <c=status>OXIDATION</c>.";

	public static string DizzyMaxArtifactName => "Dynamo";
	public static string DizzyMaxArtifactTooltip => "At the start of combat, gain a <c=card>Dynamo</c>.";
	public static string DizzyMaxArtifactCardName => "Dynamo";
	public static string DizzyMaxArtifactCardDescription => "Lose 3 <c=status>(TEMP) SHIELD</c>: gain 1 <c=status>BOOST</c>.";

	public static string DizzyPeriArtifactName => "Energy Condenser";
	public static string DizzyPeriArtifactTooltip => "Any gained <c=status>SHIELD</c> over <c=status>max shield</c> is converted into <c=status>OVERDRIVE</c> instead.\n<c=downside>Lose <c=status>SHIELD</c> equal to <c=status>OVERDRIVE</c> each turn.</c>\n<c=downside>Lose <c=status>OVERDRIVE</c> equal to <c=status>PERFECT SHIELD</c> each turn.</c>";

	public static string DizzyRiggsArtifactName => "Emergency Box";
	public static string DizzyRiggsArtifactTooltip => "Whenever you lose all <c=status>SHIELD</c>, gain 1 <c=status>EVADE</c>.";

	public static string DrakeIsaacArtifactName => "Embercore Missiles";
	public static string DrakeIsaacArtifactTooltip => "Whenever you <c=action>LAUNCH</c> a <c=midrow>midrow object</c>, lose 1 <c=status>HEAT</c>: the object gains 3 <c=midrow>SCORCHING</c>.";

	public static string DrakeMaxArtifactName => "Trojan Drive";
	public static string DrakeMaxArtifactTooltip => "At the start of combat, shuffle a <c=card>Trojan Drive</c> and a <c=cardtrait>temporary</cardtrait> <c=card>Worm</c> into your deck.";
	public static string DrakeMaxArtifactCardName => "Trojan Drive";
	public static string DrakeMaxArtifactCardDescription => "<c=cardtrait>Exhaust</c> all <c=card>Worm</c>. Apply <c=status>WORM</c> to the enemy for each.";

	public static string DrakePeriArtifactName => "Critical Mass";
	public static string DrakePeriArtifactTooltip => "Whenever you <c=action>OVERHEAT</c>, convert your <c=status>OVERDRIVE</c> into <c=status>POWERDRIVE</c>.";

	public static string DrakeRiggsArtifactName => "Backup Thrusters";
	public static string DrakeRiggsArtifactTooltip => "Once a turn, when you have no <c=status>EVADE</c>, you may still <c=status>EVADE</c>: gain 1 <c=status>HEAT</c>.";

	public static string IsaacMaxArtifactName => "Recycler";
	public static string IsaacMaxArtifactTooltip => "Whenever you <c=action>DISCARD</c> or <c=cardtrait>EXHAUST</c> any number of cards during your turn, put a <c=midrow>BUBBLE</c> on a random <c=midrow>midrow object</c> without one in front of the ship. If there are none, <c=action>LAUNCH</c> an <c=midrow>asteroid</c> on a random space in front of the ship.";

	public static string IsaacPeriArtifactName => "Enhanced Antenna";
	public static string IsaacPeriArtifactTooltip => "Your <c=midrow>Attack Drones</c> benefit from <c=status>OVERDRIVE</c>, <c=status>POWERDRIVE</c> and <c=status>FLUX</c>.";

	public static string IsaacRiggsArtifactName => "Relativistic Motion Engine";
	public static string IsaacRiggsArtifactTooltip => "<c=status>EVADE</c> and <c=status>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=status>EVADE</c> on the first turn.";

	public static string MaxPeriArtifactName => "Combat Spreadsheets";
	public static string MaxPeriArtifactTooltip => "Your leftmost <c=action>ATTACK</c> deals 1 more damage.\nYour rightmost <c=action>ATTACK</c> fires an extra 1 damage shot.\nThe last <c=action>ATTACK</c> in your hand gets no bonuses.";

	public static string MaxRiggsArtifactName => "Boba Break.EXE";
	public static string MaxRiggsArtifactTooltip => "Gain 1 <c=status>AUTOPILOT</c> each turn. <c=downside>Gain 1 <c=status>ENGINE STALL</c> each turn.</c>";

	public static string PeriRiggsArtifactName => "HARRIER Protocol";
	public static string PeriRiggsArtifactTooltip => "Gain 1 <c=status>STRAFE</c> each combat. <c=downside>You can only use 2 <c=status>EVADE</c> each turn.</c>";
}
