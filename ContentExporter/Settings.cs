using Newtonsoft.Json;

namespace Shockah.ContentExporter;

internal sealed class Settings
{
	[JsonProperty]
	public bool ExportBluish = true;
	
	[JsonProperty]
	public int CardScale = 4;
	
	[JsonProperty]
	public bool ExportCard = true;
	
	[JsonProperty]
	public bool ExportCardTooltip = true;
	
	[JsonProperty]
	public bool ExportCardUpgrades = true;
	
	[JsonProperty]
	public int ArtifactScale = 4;
	
	[JsonProperty]
	public bool ExportShip = true;
	
	[JsonProperty]
	public bool ExportShipDescription = true;
	
	[JsonProperty]
	public int ShipScale = 4;
}