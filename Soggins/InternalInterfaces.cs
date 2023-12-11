using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;

namespace Shockah.Soggins;

internal interface IRegisterableArtifact
{
	void RegisterArt(ISpriteRegistry registry);
	void RegisterArtifact(IArtifactRegistry registry);
}

internal interface IRegisterableCard
{
	void RegisterCard(ICardRegistry registry);
	void ApplyPatches(Harmony harmony) { }
}

internal interface IFrogproofCard
{
}