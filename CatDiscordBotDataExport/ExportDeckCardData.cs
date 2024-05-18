using System.Collections.Generic;

namespace Shockah.CatDiscordBotDataExport;

internal sealed record class ExportDeckCardData(
	string Key,
	string Name,
	bool PlayableCharacter,
	string? Mod,
	IReadOnlyList<ExportCardData> Cards
);