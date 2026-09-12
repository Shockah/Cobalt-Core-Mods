using System;
using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal abstract class WeaponCard : Card, IRegisterable
{
	private static readonly Dictionary<WeaponElement, ISpriteEntry> ElementCardFrames = [];
	
	protected abstract WeaponElement WeaponElement { get; }

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		foreach (var element in Enum.GetValues<WeaponElement>())
			ElementCardFrames[element] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/CardFrames/LegendaryWeapons/{Enum.GetName(element)}.png"));
	}

	internal virtual Spr OverrideCardFrame(DeckConfiguration.CardFrameOverrideArgs args)
		=> ElementCardFrames[WeaponElement].Sprite;
}