using System.Collections.Generic;

namespace Shockah.ContentExporter;

internal sealed record class ExportCardUpgradeData(
	string? Description,
	int Cost,
	IReadOnlyList<ExportCardTraitData> Traits,
	string BaseImagePath,
	string TooltipImagePath
);