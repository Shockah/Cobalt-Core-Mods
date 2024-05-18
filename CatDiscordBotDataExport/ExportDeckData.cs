using System.Collections.Generic;

namespace Shockah.CatDiscordBotDataExport;

internal sealed record class ExportDeckData(
	string Key,
	string Name,
	bool PlayableCharacter,
	string? Mod,
	IReadOnlyList<ExportCardData> Cards
);