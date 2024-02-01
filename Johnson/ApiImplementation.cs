namespace Shockah.Johnson;

public sealed class ApiImplementation : IJohnsonApi
{
	public Tooltip TemporaryUpgradeTooltip
		=> new CustomTTGlossary(
			CustomTTGlossary.GlossaryType.cardtrait,
			() => ModEntry.Instance.TemporaryUpgradeIcon.Sprite,
			() => ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "name"]),
			() => ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "description"])
		);
}
