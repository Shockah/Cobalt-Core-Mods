using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System;
using System.IO;

namespace Shockah.Shared;

public static class ISpriteRegistryExt
{
	public static Spr? TryRegisterArt(this ISpriteRegistry artRegistry, string id, FileInfo file)
	{
		ExternalSprite sprite = new(id, file);
		if (!artRegistry.RegisterArt(sprite))
			return null;
		if (sprite.Id is null)
			return null;
		return (Spr)sprite.Id;
	}

	public static Spr RegisterArtOrThrow(this ISpriteRegistry artRegistry, string id, FileInfo file)
	{
		var result = artRegistry.TryRegisterArt(id, file);
		return result ?? throw new NullReferenceException($"Failed to load asset {file.FullName}.");
	}
}