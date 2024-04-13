using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dyna;

public sealed class ShatterCharge : DynaCharge, IRegisterable
{
	private static ISpriteEntry Sprite = null!;
	private static ISpriteEntry LightsSprite = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Sprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/Shatter.png"));
		LightsSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/ShatterLight.png"));
	}

	public override Spr GetIcon(State state)
		=> Sprite.Sprite;

	public override Spr? GetLightsIcon(State state)
		=> LightsSprite.Sprite;

	public override List<Tooltip> GetTooltips(State state)
		=> [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Charge::Shatter")
			{
				Icon = GetIcon(state),
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["charge", "Shatter", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["charge", "Shatter", "description"])
			},
			new TTGlossary("parttrait.brittle")
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

		combat.QueueImmediate(new ABrittle
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