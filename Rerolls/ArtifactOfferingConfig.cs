using System.Collections.Generic;

namespace Shockah.Rerolls;

public record ArtifactOfferingConfig(
	int Count,
	Deck? LimitDeck,
	List<ArtifactPool>? LimitPools
);
