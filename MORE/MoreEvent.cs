using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shockah.MORE;

[JsonConverter(typeof(StringEnumConverter))]
internal enum MoreEvent
{
	AbyssalPower,
	ArtifactSwap,
	CombatDataCalibration,
	DraculaDeckTrial,
	ShipSwap,
}
