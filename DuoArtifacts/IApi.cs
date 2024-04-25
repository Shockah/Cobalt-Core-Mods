using CobaltCoreModding.Definitions.ExternalItems;
using System;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

public interface IApi
{
	ExternalDeck DuoArtifactDeck { get; }
	ExternalDeck TrioArtifactDeck { get; }
	ExternalDeck ComboArtifactDeck { get; }

	Deck DuoArtifactVanillaDeck { get; }
	Deck TrioArtifactVanillaDeck { get; }
	Deck ComboArtifactVanillaDeck { get; }

	DuoArtifactEligibity GetDuoArtifactEligibity(Deck deck, State state);

	bool IsDuoArtifactType(Type type);
	bool IsDuoArtifact(Artifact artifact);
	IReadOnlySet<Deck>? GetDuoArtifactTypeOwnership(Type type);
	IReadOnlySet<Deck>? GetDuoArtifactOwnership(Artifact artifact);

	IEnumerable<Type> GetAllDuoArtifactTypes();
	IEnumerable<Artifact> InstantiateAllDuoArtifacts();

	IEnumerable<Type> GetExactDuoArtifactTypes(IEnumerable<Deck> combo);
	IEnumerable<Artifact> InstantiateExactDuoArtifacts(IEnumerable<Deck> combo);

	IEnumerable<Type> GetMatchingDuoArtifactTypes(IEnumerable<Deck> combo);
	IEnumerable<Artifact> InstantiateMatchingDuoArtifacts(IEnumerable<Deck> combo);

	Color GetDynamicColorForArtifact(Artifact artifact, Deck? ignoreDeck = null);

	void RegisterDuoArtifact(Type type, IEnumerable<Deck> combo);
	void RegisterDuoArtifact<TArtifact>(IEnumerable<Deck> combo) where TArtifact : Artifact;
}

public enum DuoArtifactEligibity
{
	InvalidState, RequirementsNotSatisfied, NoDuosForThisCharacter, NoDuosForThisCrew, Eligible
}