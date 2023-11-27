using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DuoArtifactDefinition
{
	private static ModEntry Instance => ModEntry.Instance;

	public static readonly IReadOnlyList<DuoArtifactDefinition> Definitions = new List<DuoArtifactDefinition>
	{
		new(typeof(BooksDrakeArtifact), new Deck[] { Deck.shard, Deck.eunice }, I18n.BooksDrakeArtifactName, I18n.BooksDrakeArtifactTooltip, "BooksDrake", new string[] { "status.shard", "action.attackPiercing", "action.stun", "action.stunShip" }),
		new(typeof(DizzyDrakeArtifact), new Deck[] { Deck.dizzy, Deck.eunice }, I18n.DizzyDrakeArtifactName, I18n.DizzyDrakeArtifactTooltip, "DizzyDrake", new string[] { "action.overheat", "status.shieldAlt" }),
		new(typeof(DrakePeriArtifact), new Deck[] { Deck.eunice, Deck.peri }, I18n.DrakePeriArtifactName, I18n.DrakePeriArtifactTooltip, "DrakePeri", new string[] { "action.overheat", "status.overdriveAlt", "status.powerdriveAlt" }),
		new(typeof(IsaacPeriArtifact), new Deck[] { Deck.goat, Deck.peri }, I18n.IsaacPeriArtifactName, I18n.IsaacPeriArtifactTooltip, "IsaacPeri", new Func<string>[] { () => "status.overdriveAlt", () => "status.powerdriveAlt", () => Instance.FluxAltGlossaryKey }),
		new(typeof(IsaacRiggsArtifact), new Deck[] { Deck.goat, Deck.riggs }, I18n.IsaacRiggsArtifactName, I18n.IsaacRiggsArtifactTooltip, "IsaacRiggs", new string[] { "status.evade", "status.droneShift" }),
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
	public readonly IReadOnlyList<Func<string>> ExtraGlossary;

	internal readonly Lazy<HashSet<string>> CharacterKeys;

	public DuoArtifactDefinition(Type type, IEnumerable<Deck> characters, string name, string tooltip, string assetName, IEnumerable<string> extraGlossary)
		: this(type, characters, name, tooltip, assetName, extraGlossary.Select<string, Func<string>>(key => () => key)) { }

	public DuoArtifactDefinition(Type type, IEnumerable<Deck> characters, string name, string tooltip, string assetName, IEnumerable<Func<string>>? extraGlossary = null)
	{
		this.Type = type;
		this.Characters = characters.ToHashSet();
		this.Name = name;
		this.Tooltip = tooltip;
		this.AssetName = assetName;
		this.ExtraGlossary = extraGlossary?.ToList() ?? (IReadOnlyList<Func<string>>)Array.Empty<Func<string>>();
		this.CharacterKeys = new(() => this.Characters.Select(c => c.Key()).ToHashSet());
	}
}