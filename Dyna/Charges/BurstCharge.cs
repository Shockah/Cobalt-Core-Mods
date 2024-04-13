using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dyna;

public sealed class BurstCharge : DynaCharge, IRegisterable
{
	private static ISpriteEntry Sprite = null!;
	private static ISpriteEntry LightsSprite = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Sprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/Burst.png"));
		LightsSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Charges/BurstLight.png"));
	}

	public override Spr GetIcon(State state)
		=> Sprite.Sprite;

	public override Spr? GetLightsIcon(State state)
		=> LightsSprite.Sprite;

	public override List<Tooltip> GetTooltips(State state)
		=> [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Charge::Burst")
			{
				Icon = GetIcon(state),
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["charge", "Burst", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["charge", "Burst", "description"])
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
		combat.QueueImmediate(new Action
		{
			TargetPlayer = ship.isPlayerShip,
			WorldX = worldX
		});
	}

	private sealed class Action : CardAction
	{
		public bool TargetPlayer;
		public required int WorldX;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var targetShip = TargetPlayer ? s.ship : c.otherShip;
			var damageDone = targetShip.NormalDamage(s, c, 3, WorldX);
			var raycastResult = new RaycastResult
			{
				hitShip = true,
				worldX = WorldX
			};
			EffectSpawnerExt.HitEffect(MG.inst.g, TargetPlayer, raycastResult, damageDone);
		}
	}
}