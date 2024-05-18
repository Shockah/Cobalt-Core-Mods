using System.Collections.Generic;

namespace Shockah.CatDiscordBotDataExport;

internal sealed record class ExportDeckArtifactData(
	string Key,
	string Name,
	bool PlayableCharacter,
	string? Mod,
	IReadOnlyList<ExportArtifactData> Artifacts
);