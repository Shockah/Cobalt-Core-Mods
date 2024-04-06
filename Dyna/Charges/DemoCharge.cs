﻿using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dyna;

public sealed class DemoCharge : DynaCharge, IRegisterable
{
	private static ISpriteEntry Sprite = null!;
	private static ISpriteEntry LightsSprite = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Sprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/Demo.png"));
		LightsSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/DemoLight.png"));
	}

	public override Spr GetIcon(State state)
		=> Sprite.Sprite;

	public override Spr? GetLightsIcon(State state)
		=> LightsSprite.Sprite;

	public override List<Tooltip> GetTooltips(State state)
		=> [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.parttrait,
				() => GetIcon(state),
				() => ModEntry.Instance.Localizations.Localize(["charge", "Demo", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["charge", "Demo", "description"]),
				key: $"{ModEntry.Instance.Package.Manifest.UniqueName}::Charge::Demo"
			),
			new TTGlossary("parttrait.weak")
		];

	public override void OnTrigger(State state, Combat combat, Ship ship, Part part)
	{
		base.OnTrigger(state, combat, ship, part);
		if (part.type == PType.empty)
			return;

		var partIndex = ship.parts.IndexOf(part);
		if (partIndex < 0)
			return;
		var worldX = ship.x + partIndex;

		combat.QueueImmediate(new AWeaken
		{
			targetPlayer = ship.isPlayerShip,
			worldX = worldX
		});

		var damageDone = new DamageDone { hitHull = true };
		var raycastResult = new RaycastResult
		{
			hitShip = true,
			worldX = worldX
		};
		EffectSpawnerExt.HitEffect(MG.inst.g, ship.isPlayerShip, raycastResult, damageDone);
	}
}