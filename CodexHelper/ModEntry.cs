using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.CodexHelper;

public sealed class ModEntry : IModManifest
{
	public string Name { get; init; } = typeof(ModEntry).FullName!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	public void BootMod(IModLoaderContact contact)
	{
	}
}
