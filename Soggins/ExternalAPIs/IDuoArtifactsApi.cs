using CobaltCoreModding.Definitions.ExternalItems;
using System;
using System.Collections.Generic;

namespace Shockah.Soggins;

public interface IDuoArtifactsApi
{
	ExternalDeck DuoArtifactDeck { get; }

	void RegisterDuoArtifact(Type type, IEnumerable<Deck> combo);
	void RegisterDuoArtifact<TArtifact>(IEnumerable<Deck> combo) where TArtifact : Artifact;
}