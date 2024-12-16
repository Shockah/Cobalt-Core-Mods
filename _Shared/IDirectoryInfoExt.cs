using Nanoray.PluginManager;
using System;
using System.Collections.Generic;

namespace Shockah.Shared;

internal static class IDirectoryInfoExt
{
	public static IEnumerable<IFileInfo> GetSequentialFiles(this IDirectoryInfo directory, Func<int, string> fileName, int startingIndex = 0)
	{
		var index = startingIndex;
		while (true)
		{
			var indexFileName = fileName(index);
			var indexFile = directory.GetRelativeFile(indexFileName);

			if (!indexFile.Exists)
				yield break;

			yield return indexFile;
			index++;
		}
	}
}