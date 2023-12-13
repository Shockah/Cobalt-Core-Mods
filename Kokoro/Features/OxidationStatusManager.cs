using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class OxidationStatusManager : HookManager<IOxidationStatusHook>
{
	private const int BaseOxidationStatusMaxValue = 7;

	private static ModEntry Instance => ModEntry.Instance;

	public int GetOxidationStatusMaxValue(State state, Ship ship)
	{
		int value = BaseOxidationStatusMaxValue;
		foreach (var hook in GetHooksWithProxies(Instance.Api, state.EnumerateAllArtifacts()))
			value = hook.ModifyOxidationRequirement(state, ship, value);
		return value;
	}

	internal void OnTurnEnd(State state, Ship ship)
	{
		if (ship.Get((Status)Instance.Content.OxidationStatus.Id!.Value) < GetOxidationStatusMaxValue(state, ship))
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
				glossary.vals = new object[] { $"<c=boldPink>{GetOxidationStatusMaxValue(g.state, ship)}</c>" };
		}
	}
}
