using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Soggins;

internal interface IRegisterableArtifact
{
	void RegisterArt(ISpriteRegistry registry);
	void RegisterArtifact(IArtifactRegistry registry);
	void ApplyPatches(Harmony harmony) { }
	void InjectDialogue() { }
}

internal interface IRegisterableCard
{
	void RegisterArt(ISpriteRegistry registry) { }
	void RegisterCard(ICardRegistry registry);
	void ApplyPatches(Harmony harmony) { }
	void InjectDialogue() { }
}

internal interface IFrogproofCard : IHasCustomCardTraits
{
	bool IsFrogproof(State state, Combat? combat)
		=> true;

	IReadOnlySet<ICardTraitEntry> IHasCustomCardTraits.GetInnateTraits(State state)
	{
		var traits = new HashSet<ICardTraitEntry>();
		if (IsFrogproof(state, state.route as Combat))
			traits.Add(ModEntry.Instance.FrogproofTrait);
		return traits;
	}
}