namespace Shockah.Kokoro;

public sealed class OxidationStatusManager : HookManager<IOxidationStatusHook>
{
	private const int BaseOxidationStatusMaxValue = 7;

	private static ModEntry Instance => ModEntry.Instance;

	public int GetOxidationStatusMaxValue(Ship ship, State state)
	{
		int value = BaseOxidationStatusMaxValue;
		foreach (var hook in Hooks)
			value = hook.ModifyOxidationRequirement(ship, state, value);
		return value;
	}

	internal void OnTurnEnd(Ship ship, State state)
	{
		if (ship.Get((Status)Instance.Content.OxidationStatus.Id!.Value) < GetOxidationStatusMaxValue(ship, state))
			return;

		ship.Add(Status.corrode);
		ship.Set((Status)Instance.Content.OxidationStatus.Id!.Value, 0);
	}

	internal void ModifyStatusTooltips(Ship ship, G g)
	{
		for (int i = 0; i < g.tooltips.tooltips.Count; i++)
		{
			var tooltip = g.tooltips.tooltips[i];
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{Instance.Content.OxidationStatus.Id!.Value}")
				glossary.vals = new object[] { $"<c=boldPink>{GetOxidationStatusMaxValue(ship, g.state)}</c>" };
		}
	}
}
