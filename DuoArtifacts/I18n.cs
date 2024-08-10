﻿using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal static class I18n
{
	public static string DuoArtifactDeckName => "Duo";
	public static string DuoArtifactTooltip => "{0}-{1}";
	public static string DuoArtifactLongTooltip => $"{DuoArtifactTooltip} Duo Artifact.";

	public static string TrioArtifactDeckName => "Trio";
	public static string TrioArtifactTooltip => "{0}-{1}-{2}";
	public static string TrioArtifactLongTooltip => $"{TrioArtifactTooltip} Trio Artifact.";

	public static string ComboArtifactDeckName => "Combo";
	public static string ComboArtifactTooltip => "{0}";
	public static string ComboArtifactLongTooltip => $"{ComboArtifactTooltip} Combo Artifact.";
	public static string ComboArtifactTooltipSeparator => "-";

	public static string CharacterEligibleForDuoArtifact => "Eligible for duo artifacts:";
	public static string CharacterEligibleForDuoArtifactNoDuos => "Technically eligible for duo artifacts... but there are none for them!";
	public static string CharacterEligibleForDuoArtifactNoMatchingDuos => "Technically eligible for duo artifacts... but there are none for them you can get with this crew!";

	public static string GetDuoArtifactTooltip(IEnumerable<Deck> characters, bool @long = true)
	{
		var characterNamesWithColor = characters
			.Distinct()
			.Select(c => c == Deck.catartifact ? Deck.colorless : c)
			.OrderBy(NewRunOptions.allChars.IndexOf)
			.Select(c => $"<c={c.Key()}>{Loc.T($"char.{c.Key()}")}</c>")
			.ToList();

		return characterNamesWithColor.Count switch
		{
			2 => string.Format(@long ? DuoArtifactLongTooltip : DuoArtifactTooltip, characterNamesWithColor[0], characterNamesWithColor[1]),
			3 => string.Format(@long ? TrioArtifactLongTooltip : TrioArtifactTooltip, characterNamesWithColor[0], characterNamesWithColor[1], characterNamesWithColor[2]),
			_ => string.Format(@long ? ComboArtifactLongTooltip : ComboArtifactTooltip, string.Join(ComboArtifactTooltipSeparator, characterNamesWithColor))
		};
	}

	public static string ArtifactsConditionSettingName = "Crew artifacts condition";
	public static string ArtifactsConditionSettingDescription = "If enabled, duo artifacts will become available if the crew members have at least the given amount of artifacts.";
	public static string MinArtifactsSettingName = "Minimum artifacts";
	public static string RareCardsConditionSettingName = "Crew rare cards condition";
	public static string RareCardsConditionSettingDescription = "If enabled, duo artifacts will become available if the crew members have at least the given amount of rare cards.";
	public static string MinRareCardsSettingName = "Minimum rare cards";
	public static string AnyCardsConditionSettingName = "Crew cards condition";
	public static string AnyCardsConditionSettingDescription = "If enabled, duo artifacts will become available if the crew members have at least the given amount of cards of any rarity.";
	public static string MinCardsSettingName = "Minimum cards";

	public static string FluxAltGlossaryName => "Flux";
	public static string FluxAltGlossaryDescription => "Whenever this ship attacks, it gains <c=status>TEMP SHIELD</c>. <c=downside>Decreases by 1 at end of turn.</c>";
	public static CustomTTGlossary FluxAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_libra, () => FluxAltGlossaryName, () => FluxAltGlossaryDescription);

	public static string HeatAltGlossaryName => "Heat";
	public static string HeatAltGlossaryDescription => "Excess heat. If heat is too high at end of turn, <c=action>OVERHEAT</c> <c=downside>(take 1 hull damage and reset heat to 0)</c>.";
	public static CustomTTGlossary HeatAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_heat, () => HeatAltGlossaryName, () => HeatAltGlossaryDescription);

	public static string MaxShieldLowerAltGlossaryName => "Shield capacity";
	public static string MaxShieldLowerAltGlossaryDescription => "Max shield is lowered for the rest of combat.";
	public static CustomTTGlossary MaxShieldLowerAltGlossary = new(CustomTTGlossary.GlossaryType.status, () => StableSpr.icons_maxShieldLower, () => MaxShieldLowerAltGlossaryName, () => MaxShieldLowerAltGlossaryDescription);

	public static string BooksCatArtifactName => "Shard Synthesizer";
	public static string BooksCatArtifactTooltip => "At the end of each turn, gain <c=status>SHARD</c> equal to unspent <c=energy>ENERGY</c>.";

	public static string BooksDrakeArtifactName => "Stunflare Catalyst";
	public static string BooksDrakeArtifactTooltip => "Whenever you play an <c=action>attack</c>, lose 2 <c=status>SHARD</c>: the <c=action>attack</c> becomes piercing. If it was already piercing, it <c=action>stuns</c>. If it was already stunning, <c=action>TOTAL STUN</c> the opponent.";

	public static string BooksDizzyArtifactName => "Aegis Transmuter";
	public static string BooksDizzyArtifactTooltip => "<c=status>SHIELD</c> and <c=status>SHARD</c> can be used interchangeably.";

	public static string BooksIsaacArtifactName => "Drone Amplification Matrix";
	public static string BooksIsaacArtifactTooltip => "At the end of each turn, lose 2 <c=status>SHARD</c>: your <c=midrow>Attack Drones</c> deal 1 more damage.";

	public static string BooksMaxArtifactName => "Evaporator";
	public static string BooksMaxArtifactTooltip => "Whenever you <c=cardtrait>exhaust</c> a card, gain 1 <c=status>SHARD</c>.";

	public static string BooksPeriArtifactName => "Shardblade Infuser";
	public static string BooksPeriArtifactTooltip => "Whenever you play an <c=action>attack</c>, lose 1 <c=status>SHARD</c>: the <c=action>attack</c> deals 1 more damage.";

	public static string BooksRiggsArtifactName => "Fleetfoot Resonator";
	public static string BooksRiggsArtifactTooltip => "Gain 1 <c=status>HERMES BOOTS</c> for each 3 <c=status>SHARD</c> you have each turn.";

	public static string CatDizzyArtifactName => "Quantum Sanctuary";
	public static string CatDizzyArtifactTooltip => "The first time you would lose <c=status>SHIELD</c> or hull due to damage each combat, gain <c=status>PERFECT SHIELD</c> equal to your <c=status>SHIELD</c>. <c=downside>Lose ALL <c=status>max shield</c>.</c>";

	public static string CatDrakeArtifactName => "Temporal Heat Dissipator";
	public static string CatDrakeArtifactTooltip => "Whenever you gain <c=status>SERENITY</c>, gain <c=status>TIMESTOP</c>. Whenever you gain <c=status>TIMESTOP</c>, gain <c=status>SERENITY</c>.";

	public static string CatIsaacArtifactName => "Smart Launch System";
	public static string CatIsaacArtifactTooltip => "Twice per turn, whenever you are about to <c=action>launch</c> into an <c=midrow>object</c> and doing so would not benefit you, shove <c=midrow>objects</c> to the side to make space for it instead.";

	public static string CatMaxArtifactName => "Gashapon.EXE";
	public static string CatMaxArtifactTooltip => "Gain 1 random positive status each turn.";

	public static string CatPeriArtifactName => "Combat Training Simulator";
	public static string CatPeriArtifactTooltip => "Whenever you play a <c=cardtrait>temporary</c> <c=action>attack</c>, gain 1 <c=status>OVERDRIVE</c>.\n<c=downside>Lose an extra <c=status>OVERDRIVE</c> each turn.</c>";

	public static string CatRiggsArtifactName => "Tech-Support Protocol";
	public static string CatRiggsArtifactTooltip => "<c=cardtrait>Discount</c> the first non-zero-cost extra card drawn each turn.";

	public static string DizzyDrakeArtifactName => "Frozen Control Rods";
	public static string DizzyDrakeArtifactTooltip => "<c=action>OVERHEAT</c> now causes you to lose 2 <c=status>(TEMP) SHIELD</c> instead of hull, if possible.";

	public static string DizzyIsaacArtifactName => "Corrosive Payload";
	public static string DizzyIsaacArtifactTooltip => "Whenever a <c=midrow>midrow object</c> gets destroyed by an <c=action>attack</c> or <c=action>launch</c>, the ship that caused it gains 1 <c=status>OXIDATION</c>.";

	public static string DizzyMaxArtifactName => "Dynamo";
	public static string DizzyMaxArtifactTooltip => "At the start of combat, gain a <c=card>Dynamo</c>.";
	public static string DizzyMaxArtifactCardName => "Dynamo";
	public static string DizzyMaxArtifactCardDescription => "Lose 3 <c=status>(TEMP) SHIELD</c>: gain 1 <c=status>BOOST</c>.";

	public static string DizzyPeriArtifactName => "Energy Condenser";
	public static string DizzyPeriArtifactTooltip => "Any gained <c=status>SHIELD</c> over <c=status>max shield</c> is converted into <c=status>OVERDRIVE</c> instead.\n<c=downside>Lose <c=status>SHIELD</c> equal to <c=status>OVERDRIVE</c> each turn.</c>\n<c=downside>Lose <c=status>OVERDRIVE</c> equal to <c=status>PERFECT SHIELD</c> each turn.</c>";

	public static string DizzyRiggsArtifactName => "Emergency Box";
	public static string DizzyRiggsArtifactTooltip => "Whenever you lose all <c=status>SHIELD</c>, gain 1 <c=status>EVADE</c>.";

	public static string DrakeIsaacArtifactName => "Drone Overclock";
	public static string DrakeIsaacArtifactTooltip => "Your <c=midrow>Attack and Shield Drones</c> trigger twice. <c=downside>Each turn, gain <c=status>HEAT</c> equal to the number of these drones, -1.</c>";

	public static string DrakeMaxArtifactName => "Trojan Drive";
	public static string DrakeMaxArtifactTooltip => "At the start of combat, shuffle a <c=card>Trojan Drive</c> and a <c=cardtrait>temporary</cardtrait> <c=card>Worm</c> into your deck.";
	public static string DrakeMaxArtifactCardName => "Trojan Drive";
	public static string DrakeMaxArtifactCardDescription => "<c=cardtrait>Exhaust</c> all <c=card>Worm</c>. Apply <c=status>WORM</c> to the enemy for each.";

	public static string DrakePeriArtifactName => "Critical Mass";
	public static string DrakePeriArtifactTooltip => "Whenever you <c=action>OVERHEAT</c>, convert your <c=status>OVERDRIVE</c> into <c=status>POWERDRIVE</c>.";

	public static string DrakeRiggsArtifactName => "Backup Thrusters";
	public static string DrakeRiggsArtifactTooltip => "Once a turn, when you have no <c=status>EVADE</c>, you may still <c=status>EVADE</c>: gain 1 <c=status>HEAT</c>.";

	public static string IsaacMaxArtifactName => "Recycler";
	public static string IsaacMaxArtifactTooltip => "Whenever you <c=action>discard</c> or <c=cardtrait>exhaust</c> any number of cards during your turn, put a <c=midrow>bubble</c> on a random <c=midrow>midrow object</c> without one in front of the ship. If there are none, <c=action>launch</c> an <c=midrow>asteroid</c> on a random space in front of the ship.";

	public static string IsaacPeriArtifactName => "Enhanced Antenna";
	public static string IsaacPeriArtifactTooltip => "Your <c=midrow>Attack Drones</c> benefit from <c=status>OVERDRIVE</c>, <c=status>POWERDRIVE</c> and <c=status>FLUX</c>.";

	public static string IsaacRiggsArtifactName => "Relativistic Motion Engine";
	public static string IsaacRiggsArtifactTooltip => "<c=status>EVADE</c> and <c=status>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=status>EVADE</c> on the first turn.";

	public static string MaxPeriArtifactName => "Combat Spreadsheets";
	public static string MaxPeriArtifactTooltip => "Your <c=keyword>leftmost</c> <c=action>attack</c> fires an extra 1 damage shot.\nYour <c=keyword>rightmost</c> <c=action>attack</c> deals 1 more damage.\nThe last <c=action>attack</c> in your hand gets no bonuses.";

	public static string MaxRiggsArtifactName => "Boba Break.EXE";
	public static string MaxRiggsArtifactTooltip => "Gain 1 <c=status>AUTOPILOT</c> each turn. <c=downside>Gain 1 <c=status>ENGINE STALL</c> each turn.</c>";

	public static string PeriRiggsArtifactName => "HARRIER Protocol";
	public static string PeriRiggsArtifactTooltip => "Gain 1 <c=status>STRAFE</c> each combat. <c=downside>You can only use 2 <c=status>EVADE</c> each turn.</c>";
}
