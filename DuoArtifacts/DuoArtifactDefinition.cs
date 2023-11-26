using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DuoArtifactDefinition
{
	public static readonly IReadOnlyList<DuoArtifactDefinition> Definitions = new List<DuoArtifactDefinition>
	{
		new(typeof(DizzyDrakeArtifact), new Deck[] { Deck.dizzy, Deck.eunice }, I18n.DizzyDrakeArtifactName, I18n.DizzyDrakeArtifactTooltip, "DizzyDrake"),
		new(typeof(IsaacRiggsArtifact), new Deck[] { Deck.goat, Deck.riggs }, I18n.IsaacRiggsArtifactName, I18n.IsaacRiggsArtifactTooltip, "IsaacRiggs"),
	};

	public readonly Type Type;
	public readonly IReadOnlySet<Deck> Characters;
	public readonly string Name;
	public readonly string Tooltip;
	public readonly string AssetName;

	internal readonly Lazy<HashSet<string>> CharacterKeys;

	public DuoArtifactDefinition(Type type, IEnumerable<Deck> characters, string name, string tooltip, string assetName)
	{
		this.Type = type;
		this.Characters = characters.ToHashSet();
		this.Name = name;
		this.Tooltip = tooltip;
		this.AssetName = assetName;
		this.CharacterKeys = new(() => this.Characters.Select(c => c.Key()).ToHashSet());
	}
}