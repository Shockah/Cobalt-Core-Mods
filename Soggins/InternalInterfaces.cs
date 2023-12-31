﻿using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;

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

internal interface IFrogproofCard
{
	bool IsFrogproof(State state, Combat? combat)
		=> true;
}