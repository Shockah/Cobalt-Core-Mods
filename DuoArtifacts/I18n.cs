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

	public static readonly string WormStatusName = "Worm";
	public static readonly string WormStatusStatefulDescription = "Cancels {0} intents at the start of the player's turn. <c=downside>Decreases by 1 at end of turn.</c>";
	public static readonly string WormStatusStatelessDescription = "Cancels intents at the start of the player's turn. <c=downside>Decreases by 1 at end of turn.</c>";

	public static readonly string BooksCatArtifactName = "Shard Synthesizer";
	public static readonly string BooksCatArtifactTooltip = "At the end of each turn, gain <c=status>SHARD</c> equal to unspent <c=energy>ENERGY</c>.";

	public static readonly string BooksDrakeArtifactName = "Stunflare Catalyst";
	public static readonly string BooksDrakeArtifactTooltip = "Whenever you play an <c=action>attack</c>, lose 2 <c=status>SHARD</c>: the <c=action>attack</c> becomes piercing. If it was already piercing, it <c=action>stuns</c>. If it was already stunning, <c=action>TOTAL STUN</c> the opponent.";

	public static readonly string BooksDizzyArtifactName = "Aegis Transmuter";
	public static readonly string BooksDizzyArtifactTooltip = "<c=status>SHIELD</c> and <c=status>SHARD</c> can be used interchangeably.";

	public static readonly string BooksIsaacArtifactName = "Drone Amplification Matrix";
	public static readonly string BooksIsaacArtifactTooltip = "At the end of each turn, lose 2 <c=status>SHARD</c>: your <c=midrow>Attack Drones</c> deal 1 more damage.";

	public static readonly string BooksMaxArtifactName = "Evaporator";
	public static readonly string BooksMaxArtifactTooltip = "Whenever you <c=cardtrait>exhaust</c> a card, gain 1 <c=status>SHARD</c>.";

	public static readonly string BooksPeriArtifactName = "Shardblade Infuser";
	public static readonly string BooksPeriArtifactTooltip = "Whenever you play an <c=action>attack</c>, lose 1 <c=status>SHARD</c>: the <c=action>attack</c> deals 1 more damage.";

	public static readonly string BooksRiggsArtifactName = "Fleetfoot Resonator";
	public static readonly string BooksRiggsArtifactTooltip = "Gain 1 <c=status>HERMES BOOTS</c> for each 3 <c=status>SHARD</c> you have each turn.";

	public static readonly string CatDizzyArtifactName = "Quantum Sanctuary";
	public static readonly string CatDizzyArtifactTooltip = "The first time you would lose <c=status>SHIELD</c> or hull due to damage each combat, gain <c=status>PERFECT SHIELD</c> equal to your <c=status>SHIELD</c>. <c=downside>Lose ALL <c=status>max shield</c>.</c>";

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
	public static readonly string DizzyDrakeArtifactTooltip = "<c=action>OVERHEAT</c> now causes you to lose 2 <c=status>(TEMP) SHIELD</c> instead of hull, if possible.";

	public static readonly string DizzyIsaacArtifactName = "Corrosive Payload";
	public static readonly string DizzyIsaacArtifactTooltip = "Whenever a <c=midrow>midrow object</c> gets destroyed by an <c=action>attack</c> or <c=action>launch</c>, the ship that caused it gains 1 <c=status>OXIDATION</c>.";

	public static readonly string DizzyMaxArtifactName = "Dynamo";
	public static readonly string DizzyMaxArtifactTooltip = "At the start of combat, gain a <c=card>Dynamo</c>.";
	public static readonly string DizzyMaxArtifactCardName = "Dynamo";
	public static readonly string DizzyMaxArtifactCardDescription = "Lose 3 <c=status>(TEMP) SHIELD</c>: gain 1 <c=status>BOOST</c>.";

	public static readonly string DizzyPeriArtifactName = "Energy Condenser";
	public static readonly string DizzyPeriArtifactTooltip = "Any gained <c=status>SHIELD</c> over <c=status>max shield</c> is converted into <c=status>OVERDRIVE</c> instead.\n<c=downside>Lose <c=status>SHIELD</c> equal to <c=status>OVERDRIVE</c>, and lose <c=status>OVERDRIVE</c> equal to <c=status>PERFECT SHIELD</c> at the start of each turn.</c>";

	public static readonly string DizzyRiggsArtifactName = "Emergency Box";
	public static readonly string DizzyRiggsArtifactTooltip = "Whenever you lose all <c=status>SHIELD</c>, gain 1 <c=status>EVADE</c>.";

	public static readonly string DrakeIsaacArtifactName = "Drone Overclock";
	public static readonly string DrakeIsaacArtifactTooltip = "Your <c=midrow>Attack and Shield Drones</c> trigger twice. <c=downside>Each turn, gain <c=status>HEAT</c> equal to the number of these drones, -1.</c>";

	public static readonly string DrakeMaxArtifactName = "Trojan Drive";
	public static readonly string DrakeMaxArtifactTooltip = "At the start of combat, shuffle a <c=card>Trojan Drive</c> and a <c=cardtrait>temporary</cardtrait> <c=card>Worm</c> into your deck.";
	public static readonly string DrakeMaxArtifactCardName = "Trojan Drive";
	public static readonly string DrakeMaxArtifactCardDescription = "<c=cardtrait>Exhaust</c> all <c=card>Worm</c>. Apply <c=status>WORM</c> to the enemy for each.";

	public static readonly string DrakePeriArtifactName = "Critical Mass";
	public static readonly string DrakePeriArtifactTooltip = "Whenever you <c=action>OVERHEAT</c>, convert your <c=status>OVERDRIVE</c> into <c=status>POWERDRIVE</c>.";

	public static readonly string DrakeRiggsArtifactName = "Backup Thrusters";
	public static readonly string DrakeRiggsArtifactTooltip = "Once a turn, when you have no <c=status>EVADE</c>, you may still <c=status>EVADE</c>: gain 1 <c=status>HEAT</c>.";

	public static readonly string IsaacMaxArtifactName = "Recycler";
	public static readonly string IsaacMaxArtifactTooltip = "Whenever you <c=action>discard</c> or <c=cardtrait>exhaust</c> any number of cards during your turn, put a <c=midrow>bubble</c> on a random <c=midrow>midrow object</c> without one in front of the ship. If there are none, <c=action>launch</c> an <c=midrow>asteroid</c> on a random space in front of the ship.";

	public static readonly string IsaacPeriArtifactName = "Enhanced Antenna";
	public static readonly string IsaacPeriArtifactTooltip = "Your <c=midrow>Attack Drones</c> benefit from <c=status>OVERDRIVE</c>, <c=status>POWERDRIVE</c> and <c=status>FLUX</c>.";

	public static readonly string IsaacRiggsArtifactName = "Relativistic Motion Engine";
	public static readonly string IsaacRiggsArtifactTooltip = "<c=status>EVADE</c> and <c=status>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=status>EVADE</c> on the first turn.";

	public static readonly string MaxPeriArtifactName = "Combat Spreadsheets";
	public static readonly string MaxPeriArtifactTooltip = "Your <c=keyword>leftmost</c> <c=action>attack</c> fires an extra 1 damage shot.\nYour <c=keyword>rightmost</c> <c=action>attack</c> deals 1 more damage.\nThe last <c=action>attack</c> in your hand gets no bonuses.";

	public static readonly string MaxRiggsArtifactName = "Boba Break.EXE";
	public static readonly string MaxRiggsArtifactTooltip = "Gain 1 <c=status>AUTOPILOT</c> each turn. <c=downside>Gain 1 <c=status>ENGINE STALL</c> each turn.</c>";

	public static readonly string PeriRiggsArtifactName = "HARRIER Protocol";
	public static readonly string PeriRiggsArtifactTooltip = "Gain 1 <c=status>STRAFE</c> each combat. <c=downside>You can only use 2 <c=status>EVADE</c> each turn.</c>";
}
