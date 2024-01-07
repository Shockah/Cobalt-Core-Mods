#if !IS_NICKEL_MOD
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System;
using System.IO;

namespace Shockah.Shared;

internal static class ISpriteRegistryExt
{
	public static ExternalSprite? TryRegisterArt(this ISpriteRegistry artRegistry, string id, FileInfo file)
	{
		ExternalSprite sprite = new(id, file);
		if (!artRegistry.RegisterArt(sprite))
			return null;
		if (sprite.Id is null)
			return null;
		return sprite;
	}

	public static ExternalSprite RegisterArtOrThrow(this ISpriteRegistry artRegistry, string id, FileInfo file)
	{
		var result = artRegistry.TryRegisterArt(id, file);
		return result ?? throw new NullReferenceException($"Failed to load asset {file.FullName}.");
	}
}
#endif