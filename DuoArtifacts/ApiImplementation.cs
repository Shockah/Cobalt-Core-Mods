using CobaltCoreModding.Definitions.ExternalItems;
using System;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

public sealed class ApiImplementation : IApi
{
	private readonly DuoArtifactDatabase Database;

	internal ApiImplementation(DuoArtifactDatabase database)
	{
		this.Database = database;
	}

	public ExternalDeck DuoArtifactDeck
		=> Database.DuoArtifactDeck;

	public ExternalDeck TrioArtifactDeck
		=> Database.TrioArtifactDeck;

	public ExternalDeck ComboArtifactDeck
		=> Database.ComboArtifactDeck;

	public Deck DuoArtifactVanillaDeck
		=> (Deck)Database.DuoArtifactDeck.Id!.Value;

	public Deck TrioArtifactVanillaDeck
		=> (Deck)Database.TrioArtifactDeck.Id!.Value;

	public Deck ComboArtifactVanillaDeck
		=> (Deck)Database.ComboArtifactDeck.Id!.Value;

	public DuoArtifactEligibity GetDuoArtifactEligibity(Deck deck, State state)
		=> ModEntry.Instance.GetDuoArtifactEligibity(deck, state);

	public bool IsDuoArtifactType(Type type)
		=> Database.IsDuoArtifactType(type);

	public bool IsDuoArtifact(Artifact artifact)
		=> Database.IsDuoArtifact(artifact);

	public IReadOnlySet<Deck>? GetDuoArtifactTypeOwnership(Type type)
		=> Database.GetDuoArtifactTypeOwnership(type);

	public IReadOnlySet<Deck>? GetDuoArtifactOwnership(Artifact artifact)
		=> Database.GetDuoArtifactOwnership(artifact);

	public IEnumerable<Type> GetAllDuoArtifactTypes()
		=> Database.GetAllDuoArtifactTypes();

	public IEnumerable<Artifact> InstantiateAllDuoArtifacts()
		=> Database.InstantiateAllDuoArtifacts();

	public IEnumerable<Type> GetExactDuoArtifactTypes(IEnumerable<Deck> combo)
		=> Database.GetExactDuoArtifactTypes(combo);

	public IEnumerable<Artifact> InstantiateExactDuoArtifacts(IEnumerable<Deck> combo)
		=> Database.InstantiateExactDuoArtifacts(combo);

	public IEnumerable<Type> GetMatchingDuoArtifactTypes(IEnumerable<Deck> combo)
		=> Database.GetMatchingDuoArtifactTypes(combo);

	public IEnumerable<Artifact> InstantiateMatchingDuoArtifacts(IEnumerable<Deck> combo)
		=> Database.InstantiateMatchingDuoArtifacts(combo);

	public Color GetDynamicColorForArtifact(Artifact artifact, Deck? ignoreDeck = null)
		=> Database.GetDynamicColorForArtifact(artifact, ignoreDeck);

	public void RegisterDuoArtifact(Type type, IEnumerable<Deck> combo)
		=> Database.RegisterDuoArtifact(type, combo);

	public void RegisterDuoArtifact<TArtifact>(IEnumerable<Deck> combo) where TArtifact : Artifact
		=> Database.RegisterDuoArtifact(typeof(TArtifact), combo);
}