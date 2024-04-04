using Nickel;
using System.Collections.Generic;

namespace Shockah.Dyna;

internal static class ChargeExt
{
	public static DynaCharge? GetStickedCharge(this Part part)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<DynaCharge>(part, "StickedCharge");

	public static void SetStickedCharge(this Part part, DynaCharge? charge)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(part, "StickedCharge", charge);
}

internal sealed class ChargeManager
{
	public ChargeManager()
	{
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		{
			TriggerChargesIfNeeded(state, combat, part, targetPlayer: true);
		}, -1);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (State state, Combat combat, Part? part) =>
		{
			TriggerChargesIfNeeded(state, combat, part, targetPlayer: false);
		}, -1);
	}

	private static void TriggerChargesIfNeeded(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is not { } nonNullPart)
			return;
		if (nonNullPart.GetStickedCharge() is not { } charge)
			return;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		nonNullPart.SetStickedCharge(null);
		charge.OnTrigger(state, combat, targetShip, nonNullPart);
	}
}

public sealed class FireChargeAction : CardAction
{
	public required DynaCharge Charge;

	public override List<Tooltip> GetTooltips(State s)
	{
		for (var partIndex = 0; partIndex < s.ship.parts.Count; partIndex++)
		{
			var part = s.ship.parts[partIndex];
			if (part.type == PType.missiles && part.active)
				part.hilight = true;

			if (s.route is Combat combat && combat.stuff.TryGetValue(s.ship.x + partIndex, out var @object))
				@object.hilight = 2;
		}

		List<Tooltip> tooltips = [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => StableSpr.icons_spawn,
				() => ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "description"])
			)
		];
		tooltips.AddRange(Charge.GetTooltips(s));
		return tooltips;
	}
}

public abstract class DynaCharge
{
	public abstract Spr GetIcon(State state);

	public virtual Spr? GetLightsIcon(State state)
		=> null;

	public virtual void Render(G g, State state, Combat combat, Ship ship, Part part, Vec position)
	{
		var icon = GetIcon(state);
		var texture = SpriteLoader.Get(icon)!;
		Draw.Sprite(icon, position.x - texture.Width / 2.0, position.y - texture.Height / 2.0);

		if (GetLightsIcon(state) is not { } lightsIcon)
			return;
		texture = SpriteLoader.Get(lightsIcon)!;
		var color = Color.Lerp(Colors.white, Colors.black, (ModEntry.Instance.KokoroApi.TotalGameTime.TotalSeconds + position.x / 160.0) % 1.0);
		Draw.Sprite(lightsIcon, position.x - texture.Width / 2.0, position.y - texture.Height / 2.0, color: color);
	}

	public virtual List<Tooltip> GetTooltips(State state)
		=> [];

	public virtual void OnTrigger(State state, Combat combat, Ship ship, Part part)
	{
	}
}