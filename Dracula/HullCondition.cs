using System.Collections.Generic;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Dracula;

public sealed class HullCondition : IKokoroApi.IV2.IConditionalApi.IBoolExpression
{
	public required bool BelowHalf;
	public bool TargetPlayer = true;
	public bool? OverrideValue;

	public bool GetValue(State state, Combat combat)
	{
		if (OverrideValue is not null)
			return OverrideValue.Value;
		return BelowHalf ? state.ship.hull <= state.ship.hullMax / 2 : state.ship.hull > state.ship.hullMax / 2;
	}

	public string GetTooltipDescription(State state, Combat? combat)
	{
		if (state.IsOutsideRun() || state == DB.fakeState)
			return ModEntry.Instance.Localizations.Localize(["condition", "hull", BelowHalf ? "below" : "above", "stateless"]);
		else
			return ModEntry.Instance.Localizations.Localize(["condition", "hull", BelowHalf ? "below" : "above", "stateful"], new { Hull = state.ship.hullMax / 2 });
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(
				(BelowHalf ? ModEntry.Instance.HullBelowHalf : ModEntry.Instance.HullAboveHalf).Sprite,
				position.x,
				position.y,
				color: isDisabled ? Colors.disabledIconTint : Colors.white
			);
		position.x += 8;
	}

	public IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat? combat, Tooltip defaultTooltip, string defaultTooltipDescription)
		=> [
			new GlossaryTooltip($"AConditional::{ModEntry.Instance.Package.Manifest.UniqueName}::HullCondition::BelowHalf={BelowHalf}")
			{
				Icon = (BelowHalf ? ModEntry.Instance.HullBelowHalf : ModEntry.Instance.HullAboveHalf).Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["condition", "hull", BelowHalf ? "below" : "above", "title"]),
				Description = defaultTooltipDescription,
			}
		];
}