using Newtonsoft.Json;

namespace Shockah.ContentExporter;

internal sealed class Settings
{
	[JsonProperty]
	public int CardScale = 4;
	
	[JsonProperty]
	public int ArtifactScale = 4;
	
	[JsonProperty]
	public int ShipScale = 4;
}