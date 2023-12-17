using System.Collections.Generic;

namespace Shockah.CatDiscordBotDataExport;

internal sealed record class ExportDeckData(
	string Key,
	string Name,
	IReadOnlyList<ExportCardData> Cards
);