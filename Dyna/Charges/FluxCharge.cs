using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dyna;

public sealed class FluxCharge() : BaseDynaCharge($"{ModEntry.Instance.Package.Manifest.UniqueName}::FluxCharge"), IRegisterable
{
	private static ISpriteEntry Sprite = null!;
	private static ISpriteEntry LightsSprite = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Sprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/Flux.png"));
		LightsSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/FluxLight.png"));
	}

	public override Spr GetIcon(State state)
		=> Sprite.Sprite;

	public override Spr? GetLightsIcon(State state)
		=> LightsSprite.Sprite;

	public override List<Tooltip> GetTooltips(State state)
	{
		List<Tooltip> tooltips = [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Charge::Flux")
			{
				Icon = GetIcon(state),
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["charge", "Flux", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["charge", "Flux", "description"])
			}
		];
		tooltips.AddRange(FluxPartModManager.MakeFluxPartModTooltips());
		return tooltips;
	}

	public override void OnTrigger(State state, Combat combat, Ship ship, Part part)
	{
		base.OnTrigger(state, combat, ship, part);
		if (part.type == PType.empty)
			return;

		var partIndex = ship.parts.IndexOf(part);
		if (partIndex < 0)
			return;
		var worldX = ship.x + partIndex;

		combat.QueueImmediate(new Action
		{
			TargetPlayer = ship.isPlayerShip,
			TargetKey = part.key ?? "<null>",
		});

		var damageDone = new DamageDone { hitHull = true };
		var raycastResult = new RaycastResult { hitShip = true, worldX = worldX };
		EffectSpawnerExt.HitEffect(MG.inst.g, ship.isPlayerShip, raycastResult, damageDone);
	}

	private sealed class Action : CardAction
	{
		public required bool TargetPlayer;
		public required string TargetKey;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer *= 0.5;

			var targetShip = TargetPlayer ? s.ship : c.otherShip;
			if (targetShip.GetPart(TargetKey) is not { } part || part.type == PType.empty)
			{
				timer = 0;
				return;
			}

			part.damageModifier = FluxPartModManager.FluxDamageModifier;
		}
	}
}