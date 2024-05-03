#if IS_NICKEL_MOD
using Nanoray.PluginManager;
using Nickel;
using System;

namespace Shockah.Shared;

internal static class IModSpritesExt
{
	public static ISpriteEntry RegisterSpriteOrDefault(this IModSprites self, IFileInfo file, Spr @default)
		=> file.Exists ? self.RegisterSprite(file) : (self.LookupBySpr(@default) ?? throw new ArgumentException($"Unknown sprite {@default}"));
}
#endif