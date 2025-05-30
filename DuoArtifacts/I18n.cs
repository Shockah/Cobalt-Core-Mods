using System.Collections.Generic;
using System.Linq;
using Nickel;

namespace Shockah.DuoArtifacts;

internal static class I18n
{
	public static readonly string DuoArtifactDeckName = "Duo";
	public static readonly string DuoArtifactTooltip = "{0}-{1}";
	public static readonly string DuoArtifactLongTooltip = $"{DuoArtifactTooltip} Duo Artifact.";

	public static readonly string TrioArtifactDeckName = "Trio";
	public static readonly string TrioArtifactTooltip = "{0}-{1}-{2}";
	public static readonly string TrioArtifactLongTooltip = $"{TrioArtifactTooltip} Trio Artifact.";

	public static readonly string ComboArtifactDeckName = "Combo";
	public static readonly string ComboArtifactTooltip = "{0}";
	public static readonly string ComboArtifactLongTooltip = $"{ComboArtifactTooltip} Combo Artifact.";
	public static readonly string ComboArtifactTooltipSeparator = "-";

	public static readonly string CharacterEligibleForDuoArtifact = "Eligible for duo artifacts:";
	public static readonly string CharacterEligibleForDuoArtifactNoDuos = "Technically eligible for duo artifacts... but there are none for them!";
	public static readonly string CharacterEligibleForDuoArtifactNoMatchingDuos = "Technically eligible for duo artifacts... but there are none for them you can get with this crew!";

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

	public static readonly string OfferingModeSettingName = "Offering mode";
	public static readonly string OfferingModeSettingDescription = string.Join("\n", [
		"<c=textChoice>Common</c>: Duo artifacts are treated like any other Common artifacts (except having prerequisites).",
		"<c=textChoice>Extra</c>: Duo artifacts are special and will always appear as additional choices.",
		"<c=textChoice>Extra -> Common</c>: After satisfying prerequisites, Duos will always appear once as additional choices. Afterwards, they are treated like any other Common artifacts."
	]);
	public static readonly string OfferingModeSettingCommonValueName = "Common";
	public static readonly string OfferingModeSettingExtraValueName = "Extra";
	public static readonly string OfferingModeSettingExtraOnceThenCommonValueName = "Extra -> Common";
	public static readonly string ArtifactsConditionSettingName = "Crew artifacts condition";
	public static readonly string ArtifactsConditionSettingDescription = "If enabled, duo artifacts will become available if the crew members have at least the given amount of artifacts.";
	public static readonly string MinArtifactsSettingName = "Minimum artifacts";
	public static readonly string RareCardsConditionSettingName = "Crew rare cards condition";
	public static readonly string RareCardsConditionSettingDescription = "If enabled, duo artifacts will become available if the crew members have at least the given amount of rare cards.";
	public static readonly string MinRareCardsSettingName = "Minimum rare cards";
	public static readonly string AnyCardsConditionSettingName = "Crew cards condition";
	public static readonly string AnyCardsConditionSettingDescription = "If enabled, duo artifacts will become available if the crew members have at least the given amount of cards of any rarity.";
	public static readonly string MinCardsSettingName = "Minimum cards";

	public static readonly string FluxAltGlossaryName = "Flux";
	public static readonly string FluxAltGlossaryDescription = "Whenever this ship attacks, it gains <c=status>TEMP SHIELD</c>. <c=downside>Decreases by 1 at end of turn.</c>";
	public static readonly Tooltip FluxAltGlossary = new GlossaryTooltip($"status.{ModEntry.Instance.Package.Manifest.UniqueName}::FluxAlt")
	{
		Icon = StableSpr.icons_libra,
		TitleColor = Colors.status,
		Title = FluxAltGlossaryName,
		Description = FluxAltGlossaryDescription,
	};

	public static readonly string HeatAltGlossaryName = "Heat";
	public static readonly string HeatAltGlossaryDescription = "Excess heat. If heat is too high at end of turn, <c=action>OVERHEAT</c> <c=downside>(take 1 hull damage and reset heat to 0)</c>.";
	public static readonly Tooltip HeatAltGlossary = new GlossaryTooltip($"status.{ModEntry.Instance.Package.Manifest.UniqueName}::HeatAlt")
	{
		Icon = StableSpr.icons_heat,
		TitleColor = Colors.status,
		Title = HeatAltGlossaryName,
		Description = HeatAltGlossaryDescription,
	};

	public static readonly string MaxShieldLowerAltGlossaryName = "Shield capacity";
	public static readonly string MaxShieldLowerAltGlossaryDescription = "Max shield is lowered for the rest of combat.";
	public static readonly Tooltip MaxShieldLowerAltGlossary = new GlossaryTooltip($"status.{ModEntry.Instance.Package.Manifest.UniqueName}::MaxShieldLowerAlt")
	{
		Icon = StableSpr.icons_maxShieldLower,
		TitleColor = Colors.status,
		Title = MaxShieldLowerAltGlossaryName,
		Description = MaxShieldLowerAltGlossaryDescription,
	};

	public static readonly string BooksCatArtifactName = "Shard Synthesizer";
	public static readonly string BooksCatArtifactTooltip = "At the end of each turn, gain <c=status>SHARD</c> equal to unspent <c=energy>ENERGY</c>.";

	public static readonly string BooksDrakeArtifactName = "Stunflare Catalyst";
	public static readonly string BooksDrakeArtifactTooltip = "Whenever you <c=action>attack</c>, spend 2 <c=status>SHARD</c> to make the <c=action>attack</c> pierce. If it was already piercing, it <c=action>stuns</c>. If it was already stunning, <c=action>TOTAL STUN</c> the opponent.";

	public static readonly string BooksDizzyArtifactName = "Aegis Transmuter";
	public static readonly string BooksDizzyArtifactTooltip = "<c=status>SHIELD</c> and <c=status>SHARD</c> can be used interchangeably.";

	public static readonly string BooksIsaacArtifactName = "Drone Amplification Matrix";
	public static readonly string BooksIsaacArtifactTooltip = "At the end of each turn, spend 2 <c=status>SHARD</c> to make your <c=midrow>Attack Drones</c> deal 1 more damage this turn.";

	public static readonly string BooksMaxArtifactName = "Evaporator";
	public static readonly string BooksMaxArtifactTooltip = "Whenever you <c=cardtrait>exhaust</c> a card, gain 1 <c=status>SHARD</c>.";

	public static readonly string BooksPeriArtifactName = "Shardblade Infuser";
	public static readonly string BooksPeriArtifactTooltip = "Whenever you play an <c=action>attack</c> card while having 3 <c=status>SHARD</c>, all <c=action>attacks</c> of that card deal 1 more damage.";

	public static readonly string BooksRiggsArtifactName = "Fleetfoot Resonator";
	public static readonly string BooksRiggsArtifactTooltip = "Gain 1 <c=status>HERMES BOOTS</c> for each 3 <c=status>SHARD</c> you have each turn.";

	public static readonly string CatDizzyArtifactName = "Quantum Sanctuary";
	public static readonly string CatDizzyArtifactTooltip = "The first time you would lose <c=status>SHIELD</c> or hull due to damage each combat, gain <c=status>PERFECT SHIELD</c> equal to your <c=status>SHIELD</c>. <c=downside>Lose ALL but 1 <c=keyword>max shield</c>.</c>";

	public static readonly string CatDrakeArtifactName = "Temporal Heat Dissipator";
	public static readonly string CatDrakeArtifactTooltip = "Whenever you gain <c=status>SERENITY</c>, gain <c=status>TIMESTOP</c>. Whenever you gain <c=status>TIMESTOP</c>, gain <c=status>SERENITY</c>.";

	public static readonly string CatIsaacArtifactName = "Smart Launch System";
	public static readonly string CatIsaacArtifactTooltip = "Twice per turn, whenever you are about to <c=action>launch</c> into an <c=midrow>object</c> and doing so would not benefit you, shove <c=midrow>objects</c> to the side to make space for it instead.";

	public static readonly string CatMaxArtifactName = "Gashapon.EXE";
	public static readonly string CatMaxArtifactTooltip = "Gain 1 random positive status each turn.";

	public static readonly string CatPeriArtifactName = "Combat Training Simulator";
	public static readonly string CatPeriArtifactTooltip = "Whenever you play a <c=cardtrait>temporary</c> <c=action>attack</c>, gain 1 <c=status>OVERDRIVE</c>.\n<c=downside>Lose an extra <c=status>OVERDRIVE</c> each turn.</c>";

	public static readonly string CatRiggsArtifactName = "Tech-Support Protocol";
	public static readonly string CatRiggsArtifactTooltip = "<c=cardtrait>Discount</c> the first non-zero-cost extra card drawn each turn.";

	public static readonly string DizzyDrakeArtifactName = "Frozen Control Rods";
	public static readonly string DizzyDrakeArtifactTooltip = "<c=downside>Overheating</c> now causes you to lose 2 <c=status>(TEMP) SHIELD</c> instead of any hull, if possible.";

	public static readonly string DizzyIsaacArtifactName = "Corrosive Payload";
	public static readonly string DizzyIsaacArtifactTooltip = "Whenever a <c=midrow>midrow object</c> gets destroyed by an <c=action>attack</c> or <c=action>launch</c>, the ship that caused it gains 2 <c=status>OXIDATION</c>.";

	public static readonly string DizzyMaxArtifactName = "Dynamo";
	public static readonly string DizzyMaxArtifactTooltip = "At the start of combat, gain a <c=card>Dynamo</c>.";
	public static readonly string DizzyMaxArtifactCardName = "Dynamo";
	public static readonly string DizzyMaxArtifactCardDescription = "Lose 3 <c=status>(temp) shield</c>: gain 1 <c=status>boost</c>.";

	public static readonly string DizzyPeriArtifactName = "Energy Condenser";
	public static readonly string DizzyPeriArtifactTooltip = "Whenever you gain any amount of <c=status>SHIELD</c> over capacity, gain 1 <c=status>OVERDRIVE</c>.";

	public static readonly string DizzyRiggsArtifactName = "Emergency Box";
	public static readonly string DizzyRiggsArtifactTooltip = "Whenever you lose all <c=status>SHIELD</c>, gain 1 <c=status>EVADE</c>.";

	public static readonly string DrakeIsaacArtifactName = "Drone Overclock";
	public static readonly string DrakeIsaacArtifactTooltip = "Your <c=midrow>Attack and Shield Drones</c> trigger twice. <c=downside>Each turn, gain <c=status>HEAT</c> equal to the number of these drones, -1.</c>";

	public static readonly string DrakeMaxArtifactName = "CO2 Fire Extinguisher";
	public static readonly string DrakeMaxArtifactTooltip = "After you <c=cardtrait>exhaust</c> a card while you are about to <c=downside>overheat</c>, reduce your <c=status>HEAT</c> to right below the <c=downside>overheat</c> threshold.";

	public static readonly string DrakePeriArtifactName = "Critical Mass";
	public static readonly string DrakePeriArtifactTooltip = "Whenever you <c=downside>overheat</c>, convert your <c=status>OVERDRIVE</c> into <c=status>POWERDRIVE</c>.";

	public static readonly string DrakeRiggsArtifactName = "Backup Thrusters";
	public static readonly string DrakeRiggsArtifactTooltip = "Once per turn, when you have no <c=status>EVADE</c> left to spend, you may still <c=status>EVADE</c> at the cost of gaining 1 <c=status>HEAT</c>.";

	public static readonly string IsaacMaxArtifactName = "Recycler";
	public static readonly string IsaacMaxArtifactTooltip = "Whenever you <c=action>discard</c> or <c=cardtrait>exhaust</c> any number of cards during your turn, put a <c=midrow>bubble</c> on a random <c=midrow>midrow object</c> without one in front of the ship. If there are none, <c=action>launch</c> an <c=midrow>asteroid</c> on a random space in front of the ship.";

	public static readonly string IsaacPeriArtifactName = "Enhanced Antenna";
	public static readonly string IsaacPeriArtifactTooltip = "Your <c=midrow>Attack Drones</c> benefit from <c=status>FLUX</c>, <c=status>OVERDRIVE</c> and <c=status>POWERDRIVE</c>.";

	public static readonly string IsaacRiggsArtifactName = "Relativistic Motion Engine";
	public static readonly string IsaacRiggsArtifactTooltip = "<c=status>DRONESHIFT</c> and <c=status>EVADE</c> can be used interchangeably.\nGain 1 <c=status>EVADE</c> on the first turn.";

	public static readonly string MaxPeriArtifactName = "Combat Spreadsheets";
	public static readonly string MaxPeriArtifactTooltip = "Your <c=keyword>leftmost</c> <c=action>attack</c> card fires an extra 1 damage shot.\nYour <c=keyword>rightmost</c> <c=action>attack</c> card deals 1 more damage.\nThe last <c=action>attack</c> card in your hand gets no bonuses.";

	public static readonly string MaxRiggsArtifactName = "Boba Break.EXE";
	public static readonly string MaxRiggsArtifactTooltip = "Gain 1 <c=status>AUTOPILOT</c> each turn. <c=downside>Gain 1 <c=status>ENGINE STALL</c> each turn.</c>";

	public static readonly string PeriRiggsArtifactName = "HARRIER Protocol";
	public static readonly string PeriRiggsArtifactTooltip = "Gain 1 <c=status>STRAFE</c> each combat. <c=downside>You can only use 2 <c=status>EVADE</c> each turn.</c>";
}
