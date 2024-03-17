using CobaltCoreModding.Definitions.ExternalItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DuoArtifactDatabase
{
	internal ExternalDeck DuoArtifactDeck { get; set; } = null!;
	internal ExternalDeck TrioArtifactDeck { get; set; } = null!;
	internal ExternalDeck ComboArtifactDeck { get; set; } = null!;
	private readonly Dictionary<HashSet<string>, HashSet<Type>> ComboToTypeDictionary = new(HashSet<string>.CreateSetComparer());
	private readonly Dictionary<Type, HashSet<string>> TypeToComboDictionary = [];

	public bool IsDuoArtifactType(Type type)
		=> TypeToComboDictionary.ContainsKey(type);

	public bool IsDuoArtifact(Artifact artifact)
		=> IsDuoArtifactType(artifact.GetType());

	public IReadOnlySet<Deck>? GetDuoArtifactTypeOwnership(Type type)
		=> TypeToComboDictionary.GetValueOrDefault(type)?.Select(key => DB.decks.Keys.First(deck => deck.Key() == key)).ToHashSet();

	public IReadOnlySet<Deck>? GetDuoArtifactOwnership(Artifact artifact)
		=> GetDuoArtifactTypeOwnership(artifact.GetType());

	public IEnumerable<Type> GetAllDuoArtifactTypes()
		=> TypeToComboDictionary.Keys;

	public IEnumerable<Artifact> InstantiateAllDuoArtifacts()
		=> TypeToComboDictionary.Keys.Select(t => (Artifact)Activator.CreateInstance(t)!);

	public IEnumerable<Type> GetExactDuoArtifactTypes(IEnumerable<Deck> combo)
		=> ComboToTypeDictionary.TryGetValue(FixCombo(combo).Select(d => d.Key()).ToHashSet(), out var types) ? types : Array.Empty<Type>();

	public IEnumerable<Artifact> InstantiateExactDuoArtifacts(IEnumerable<Deck> combo)
		=> GetExactDuoArtifactTypes(combo).Select(t => (Artifact)Activator.CreateInstance(t)!);

	public IEnumerable<Type> GetMatchingDuoArtifactTypes(IEnumerable<Deck> combo)
	{
		var comboKeySet = FixCombo(combo).Select(d => d.Key()).ToHashSet();
		foreach (var (keySet, types) in ComboToTypeDictionary)
			if (!keySet.Except(comboKeySet).Any())
				foreach (var type in types)
					yield return type;
	}

	public IEnumerable<Artifact> InstantiateMatchingDuoArtifacts(IEnumerable<Deck> combo)
		=> GetMatchingDuoArtifactTypes(combo).Select(t => (Artifact)Activator.CreateInstance(t)!);

	public void RegisterDuoArtifact(Type type, IEnumerable<Deck> combo)
	{
		if (TypeToComboDictionary.ContainsKey(type))
			throw new ArgumentException($"Artifact type {type} is already registered as a duo.");
		if (!type.IsAssignableTo(typeof(Artifact)))
			throw new ArgumentException($"Type {type} is not a subclass of the {typeof(Artifact)} type.");

		var comboKeySet = FixCombo(combo).Select(d => d.Key()).ToHashSet();
		if (comboKeySet.Count < 2)
			throw new ArgumentException("Tried to register a duo artifact for less than 2 characters.");
		TypeToComboDictionary[type] = comboKeySet;

		if (!ComboToTypeDictionary.TryGetValue(comboKeySet, out var types))
		{
			types = [];
			ComboToTypeDictionary[comboKeySet] = types;
		}
		types.Add(type);
	}

	private static IEnumerable<Deck> FixCombo(IEnumerable<Deck> combo)
		=> combo.Select(d => d == Deck.catartifact ? Deck.colorless : d);
}