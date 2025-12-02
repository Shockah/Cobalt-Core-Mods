using System.Collections.Generic;

namespace Shockah.ContentExporter;

internal sealed record class ExportCardData(
	string Key,
	string Name,
	Rarity Rarity,
	bool Released,
	bool Offered,
	string? Mod,
	IReadOnlyDictionary<Upgrade, ExportCardUpgradeData> Upgrades
);