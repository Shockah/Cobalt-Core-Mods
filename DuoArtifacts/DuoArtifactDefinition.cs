using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DuoArtifactDefinition
{
	public static readonly IReadOnlyList<DuoArtifactDefinition> Definitions = new List<DuoArtifactDefinition>
	{
		new(typeof(BooksCatArtifact), new Deck[] { Deck.shard, Deck.catartifact }, I18n.BooksCatArtifactName, I18n.BooksCatArtifactTooltip, "BooksCat", "status.shard"),
		new(typeof(BooksDizzyArtifact), new Deck[] { Deck.shard, Deck.dizzy }, I18n.BooksDizzyArtifactName, I18n.BooksDizzyArtifactTooltip, "BooksDizzy", "status.shieldAlt", "status.shard"),
		new(typeof(BooksDrakeArtifact), new Deck[] { Deck.shard, Deck.eunice }, I18n.BooksDrakeArtifactName, I18n.BooksDrakeArtifactTooltip, "BooksDrake", "status.shard", "action.attackPiercing", "action.stun", "action.stunShip"),
		new(typeof(BooksMaxArtifact), new Deck[] { Deck.shard, Deck.hacker }, I18n.BooksMaxArtifactName, I18n.BooksMaxArtifactTooltip, "BooksMax", "cardtrait.exhaust", "status.shard"),
		new(typeof(BooksRiggsArtifact), new Deck[] { Deck.shard, Deck.riggs }, I18n.BooksRiggsArtifactName, I18n.BooksRiggsArtifactTooltip, "BooksRiggs", new TTGlossary("status.hermes", 1), "status.shard"),
		new(typeof(CatDizzyArtifact), new Deck[] { Deck.catartifact, Deck.dizzy }, I18n.CatDizzyArtifactName, I18n.CatDizzyArtifactTooltip, "CatDizzy", "status.shieldAlt", "status.perfectShield", I18n.MaxShieldLowerAltGlossary),
		new(typeof(CatDrakeArtifact), new Deck[] { Deck.catartifact, Deck.eunice }, I18n.CatDrakeArtifactName, I18n.CatDrakeArtifactTooltip, "CatDrake", "status.serenity", "status.timeStop"),
		new(typeof(CatIsaacArtifact), new Deck[] { Deck.catartifact, Deck.goat }, I18n.CatIsaacArtifactName, I18n.CatIsaacArtifactTooltip, "CatIsaac", "action.spawn", I18n.DroneMoveGlossary),
		new(typeof(CatMaxArtifact), new Deck[] { Deck.catartifact, Deck.hacker }, I18n.CatMaxArtifactName, I18n.CatMaxArtifactTooltip, "CatMax"),
		new(typeof(CatPeriArtifact), new Deck[] { Deck.catartifact, Deck.peri }, I18n.CatPeriArtifactName, I18n.CatPeriArtifactTooltip, "CatPeri", "cardtrait.temporary", "status.overdriveAlt"),
		new(typeof(CatRiggsArtifact), new Deck[] { Deck.catartifact, Deck.riggs }, I18n.CatRiggsArtifactName, I18n.CatRiggsArtifactTooltip, "CatRiggs", new TTGlossary("cardtrait.discount", 1)),
		new(typeof(DizzyDrakeArtifact), new Deck[] { Deck.dizzy, Deck.eunice }, I18n.DizzyDrakeArtifactName, I18n.DizzyDrakeArtifactTooltip, "DizzyDrake", "action.overheat", "status.shieldAlt"),
		new(typeof(DizzyMaxArtifact), new Deck[] { Deck.dizzy, Deck.hacker }, I18n.DizzyMaxArtifactName, I18n.DizzyMaxArtifactTooltip, "DizzyMax", "status.shieldAlt", new TTGlossary("status.boost", 1)),
		new(typeof(DizzyPeriArtifact), new Deck[] { Deck.dizzy, Deck.peri }, I18n.DizzyPeriArtifactName, I18n.DizzyPeriArtifactTooltip, "DizzyPeri", "status.shieldAlt", "status.overdriveAlt"),
		new(typeof(DrakeIsaacArtifact), new Deck[] { Deck.eunice, Deck.goat }, I18n.DrakeIsaacArtifactName, I18n.DrakeIsaacArtifactTooltip, "DrakeIsaac", "action.spawn", I18n.ScorchingGlossary, I18n.HeatAltGlossary),
		new(typeof(DrakeMaxArtifact), new Deck[] { Deck.eunice, Deck.hacker }, I18n.DrakeMaxArtifactName, I18n.DrakeMaxArtifactTooltip, "DrakeMax"),
		new(typeof(DrakePeriArtifact), new Deck[] { Deck.eunice, Deck.peri }, I18n.DrakePeriArtifactName, I18n.DrakePeriArtifactTooltip, "DrakePeri", "action.overheat", "status.overdriveAlt", "status.powerdriveAlt"),
		new(typeof(DrakeRiggsArtifact), new Deck[] { Deck.eunice, Deck.riggs }, I18n.DrakeRiggsArtifactName, I18n.DrakeRiggsArtifactTooltip, "DrakeRiggs", "status.evade", I18n.HeatAltGlossary),
		new(typeof(IsaacMaxArtifact), new Deck[] { Deck.goat, Deck.hacker }, I18n.IsaacMaxArtifactName, I18n.IsaacMaxArtifactTooltip, "IsaacMax", "cardtrait.exhaust", "midrow.bubbleShield", "action.spawn", "midrow.asteroid"),
		new(typeof(IsaacPeriArtifact), new Deck[] { Deck.goat, Deck.peri }, I18n.IsaacPeriArtifactName, I18n.IsaacPeriArtifactTooltip, "IsaacPeri", "status.overdriveAlt", "status.powerdriveAlt", I18n.FluxAltGlossary),
		new(typeof(IsaacRiggsArtifact), new Deck[] { Deck.goat, Deck.riggs }, I18n.IsaacRiggsArtifactName, I18n.IsaacRiggsArtifactTooltip, "IsaacRiggs", "status.evade", "status.droneShift"),
		new(typeof(MaxPeriArtifact), new Deck[] { Deck.hacker, Deck.peri }, I18n.MaxPeriArtifactName, I18n.MaxPeriArtifactTooltip, "MaxPeri"),
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

		public DefinitionTooltip(Tooltip tooltip)
		{
			this.TooltipFactory = () => tooltip;
		}

		public DefinitionTooltip(string glossaryKey) : this(new TTGlossary(glossaryKey)) { }

		public Tooltip MakeTooltip()
			=> TooltipFactory();

		public static implicit operator DefinitionTooltip(Func<DefinitionTooltip> lazyFunction)
			=> new(lazyFunction);

		public static implicit operator DefinitionTooltip(Tooltip tooltip)
			=> new(tooltip);

		public static implicit operator DefinitionTooltip(string glossaryKey)
			=> new(glossaryKey);
	}
}