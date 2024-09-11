using CobaltCoreModding.Definitions.ExternalItems;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public ExternalStatus WormStatus
		=> Instance.Content.WormStatus;

	public Status WormVanillaStatus
		=> (Status)WormStatus.Id!.Value;

	public Tooltip GetWormStatusTooltip(int? value = null)
		=> value is null
			? new CustomTTGlossary(CustomTTGlossary.GlossaryType.status, () => (Spr)Instance.Content.WormSprite.Id!.Value, () => I18n.WormStatusName, () => I18n.WormStatusAltGlossaryDescription)
			: new TTGlossary($"status.{Instance.Content.WormStatus.Id!.Value}", value.Value);
}

internal sealed class WormStatusManager : IStatusLogicHook, IStatusRenderHook
{
	internal static readonly WormStatusManager Instance = new();

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		foreach (var tooltip in tooltips)
		{
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{ModEntry.Instance.Content.WormStatus.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
				glossary.vals = ["<c=boldPink>1</c>"];
		}
		return tooltips;
	}

	public void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != (Status)ModEntry.Instance.Content.WormStatus.Id!.Value || timing != StatusTurnTriggerTiming.TurnStart)
			return;

		var otherShip = ship.isPlayerShip ? combat.otherShip : state.ship;
		var wormAmount = otherShip.Get((Status)ModEntry.Instance.Content.WormStatus.Id!.Value);
		if (wormAmount <= 0)
			return;

		if (!otherShip.isPlayerShip)
		{
			var partXsWithIntent = Enumerable.Range(0, otherShip.parts.Count)
				.Where(x => otherShip.parts[x].intent is not null)
				.Select(x => x + otherShip.x)
				.ToList();

			foreach (var partXWithIntent in partXsWithIntent.Shuffle(state.rngActions).Take(wormAmount))
				combat.Queue(new AStunPart { worldX = partXWithIntent });
		}

		combat.Queue(new AStatus
		{
			targetPlayer = otherShip.isPlayerShip,
			status = (Status)ModEntry.Instance.Content.WormStatus.Id!.Value,
			statusAmount = -1
		});
	}
}
