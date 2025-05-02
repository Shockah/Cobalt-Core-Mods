using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dyna;

public sealed class SwiftCharge() : BaseDynaCharge($"{ModEntry.Instance.Package.Manifest.UniqueName}::SwiftCharge"), IRegisterable
{
	private static ISpriteEntry Sprite = null!;
	private static ISpriteEntry LightsSprite = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Sprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/Swift.png"));
		LightsSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/SwiftLight.png"));
	}

	public override Spr GetIcon(State state)
		=> Sprite.Sprite;

	public override Spr? GetLightsIcon(State state)
		=> LightsSprite.Sprite;

	public override List<Tooltip> GetTooltips(State state)
		=> [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Charge::Swift")
			{
				Icon = GetIcon(state),
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["charge", "Swift", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["charge", "Swift", "description"])
			}
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

		combat.QueueImmediate([
			new ADrawCard { count = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
		]);

		var damageDone = new DamageDone { hitHull = true };
		var raycastResult = new RaycastResult { hitShip = true, worldX = worldX };
		EffectSpawnerExt.HitEffect(MG.inst.g, ship.isPlayerShip, raycastResult, damageDone);
	}
}