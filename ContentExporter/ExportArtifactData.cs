namespace Shockah.ContentExporter;

internal sealed record class ExportArtifactData(
	string Key,
	string Name,
	string Description,
	bool Common,
	bool Boss,
	bool EventOnly,
	bool Released,
	bool Removable,
	string? Mod,
	string TooltipImagePath
);