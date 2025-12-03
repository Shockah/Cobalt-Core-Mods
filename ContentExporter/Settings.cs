using System.Collections.Generic;
using Newtonsoft.Json;

namespace Shockah.ContentExporter;

internal sealed class Settings
{
	internal const int DEFAULT_SCALE = 4;
	
	[JsonProperty]
	public bool ScreenFilter = true;
	
	[JsonProperty]
	public ExportBackground Background = ExportBackground.Transparent;
	
	[JsonProperty]
	public int? CardsScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int? CardTooltipsScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int? CardUpgradesScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int? ShipsScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int? ShipDescriptionsScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int? ArtifactsScale = DEFAULT_SCALE;
	
	[JsonIgnore]
	public readonly HashSet<string> FilterToMods = [];
	
	[JsonProperty]
	public bool FilterToRun;
}