using System.Collections.Generic;

namespace Shockah.CatDiscordBotDataExport;

internal sealed record class ExportCardData(
	string Key,
	string Name,
	bool Unreleased,
	Rarity Rarity,
	IReadOnlySet<Upgrade> Upgrades,
	string? Description
);