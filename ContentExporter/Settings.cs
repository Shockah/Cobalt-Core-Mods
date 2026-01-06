using System.Collections.Generic;
using Newtonsoft.Json;

namespace Shockah.ContentExporter;

internal sealed partial class Settings
{
	internal const int DEFAULT_SCALE = 4;
	
	[JsonProperty]
	public bool ScreenFilter = true;
	
	[JsonProperty]
	public ExportBackground Background = ExportBackground.Transparent;
	
	[JsonIgnore]
	public readonly HashSet<string> FilterToMods = [];
	
	[JsonProperty]
	public bool FilterToRun;
}