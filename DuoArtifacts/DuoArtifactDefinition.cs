using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DuoArtifactDefinition
{
	public static readonly IReadOnlyList<DuoArtifactDefinition> Definitions = new List<DuoArtifactDefinition>
	{
		new(typeof(BooksDizzyArtifact), new Deck[] { Deck.shard, Deck.dizzy }, I18n.BooksDizzyArtifactName, I18n.BooksDizzyArtifactTooltip, "BooksDizzy", "status.shieldAlt", "status.shard"),
		new(typeof(BooksDrakeArtifact), new Deck[] { Deck.shard, Deck.eunice }, I18n.BooksDrakeArtifactName, I18n.BooksDrakeArtifactTooltip, "BooksDrake", "status.shard", "action.attackPiercing", "action.stun", "action.stunShip"),
		new(typeof(CatMaxArtifact), new Deck[] { Deck.catartifact, Deck.hacker }, I18n.CatMaxArtifactName, I18n.CatMaxArtifactTooltip, "CatMax"),
		new(typeof(DizzyDrakeArtifact), new Deck[] { Deck.dizzy, Deck.eunice }, I18n.DizzyDrakeArtifactName, I18n.DizzyDrakeArtifactTooltip, "DizzyDrake", "action.overheat", "status.shieldAlt"),
		new(typeof(DizzyPeriArtifact), new Deck[] { Deck.dizzy, Deck.peri }, I18n.DizzyPeriArtifactName, I18n.DizzyPeriArtifactTooltip, "DizzyPeri", "status.shieldAlt", "status.overdriveAlt"),
		new(typeof(DrakePeriArtifact), new Deck[] { Deck.eunice, Deck.peri }, I18n.DrakePeriArtifactName, I18n.DrakePeriArtifactTooltip, "DrakePeri", "action.overheat", "status.overdriveAlt", "status.powerdriveAlt"),
		new(typeof(DrakeRiggsArtifact), new Deck[] { Deck.eunice, Deck.riggs }, I18n.DrakeRiggsArtifactName, I18n.DrakeRiggsArtifactTooltip, "DrakeRiggs", "status.evade", I18n.HeatAltGlossary),
		new(typeof(IsaacMaxArtifact), new Deck[] { Deck.goat, Deck.hacker }, I18n.IsaacMaxArtifactName, I18n.IsaacMaxArtifactTooltip, "IsaacMax", "cardtrait.exhaust", "midrow.bubbleShield", "action.spawn", "midrow.asteroid"),
		new(typeof(IsaacPeriArtifact), new Deck[] { Deck.goat, Deck.peri }, I18n.IsaacPeriArtifactName, I18n.IsaacPeriArtifactTooltip, "IsaacPeri", "status.overdriveAlt", "status.powerdriveAlt", I18n.FluxAltGlossary),
		new(typeof(IsaacRiggsArtifact), new Deck[] { Deck.goat, Deck.riggs }, I18n.IsaacRiggsArtifactName, I18n.IsaacRiggsArtifactTooltip, "IsaacRiggs", "status.evade", "status.droneShift"),
		new(typeof(MaxRiggsArtifact), new Deck[] { Deck.hacker, Deck.riggs }, I18n.MaxRiggsArtifactName, I18n.MaxRiggsArtifactTooltip, "MaxRiggs"),
	};

	private static readonly Dictionary<Type, DuoArtifactDefinition> TypeToDefinitionDictionary = new();

	static DuoArtifactDefinition()
	{
		foreach (var definition in Definitions)
			TypeToDefinitionDictionary[definition.Type] = definition;
	}

	public static DuoArtifactDefinition? GetDefinition(Type type)
		=> type.IsAssignableTo(typeof(DuoArtifact)) ? TypeToDefinitionDictionary.GetValueOrDefault(type) : null;

	public static DuoArtifactDefinition? GetDefinition<TType>() where TType : DuoArtifact
		=> TypeToDefinitionDictionary.GetValueOrDefault(typeof(TType));

	public readonly Type Type;
	public readonly IReadOnlySet<Deck> Characters;
	public readonly string Name;
	public readonly string Tooltip;
	public readonly string AssetName;
	public readonly IReadOnlyList<DefinitionTooltip> ExtraTooltips;

	internal readonly Lazy<HashSet<string>> CharacterKeys;

	public DuoArtifactDefinition(Type type, IEnumerable<Deck> characters, string name, string tooltip, string assetName, params DefinitionTooltip[] extraTooltips)
	{
		this.Type = type;
		this.Characters = characters.ToHashSet();
		this.Name = name;
		this.Tooltip = tooltip;
		this.AssetName = assetName;
		this.ExtraTooltips = extraTooltips;
		this.CharacterKeys = new(() => this.Characters.Select(c => c.Key()).ToHashSet());
	}

	public sealed class DefinitionTooltip
	{
		private readonly Func<Tooltip> TooltipFactory;

		public DefinitionTooltip(Func<DefinitionTooltip> lazyFunction)
		{
			this.TooltipFactory = () => lazyFunction().MakeTooltip();
		}

		public DefinitionTooltip(string glossaryKey)
		{
			this.TooltipFactory = () => new TTGlossary(glossaryKey);
		}

		public DefinitionTooltip(CustomTTGlossary glossary)
		{
			this.TooltipFactory = () => glossary;
		}

		public Tooltip MakeTooltip()
			=> TooltipFactory();

		public static implicit operator DefinitionTooltip(Func<DefinitionTooltip> lazyFunction)
			=> new(lazyFunction);

		public static implicit operator DefinitionTooltip(string glossaryKey)
			=> new(glossaryKey);

		public static implicit operator DefinitionTooltip(CustomTTGlossary glossary)
			=> new(glossary);
	}
}