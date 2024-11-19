using CobaltCoreModding.Definitions.ExternalItems;
using Nickel;
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
			? new GlossaryTooltip($"status.{Instance.Content.WormStatus.Id!.Value}")
			{
				Icon = (Spr)Instance.Content.WormSprite.Id!.Value,
				TitleColor = Colors.status,
				Title = ModEntry.Instance.Localizations.Localize(["status", "Worm", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["status", "Worm", "description", "stateless"]),
			} : new TTGlossary($"status.{Instance.Content.WormStatus.Id!.Value}", value.Value);
}

internal sealed class WormStatusManager : IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static readonly WormStatusManager Instance = new();

	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		foreach (var tooltip in args.Tooltips)
		{
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{ModEntry.Instance.Content.WormStatus.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
				glossary.vals = ["<c=boldPink>1</c>"];
		}
		return args.Tooltips;
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status != (Status)ModEntry.Instance.Content.WormStatus.Id!.Value || args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return;

		var otherShip = args.Ship.isPlayerShip ? args.Combat.otherShip : args.State.ship;
		var wormAmount = otherShip.Get((Status)ModEntry.Instance.Content.WormStatus.Id!.Value);
		if (wormAmount <= 0)
			return;

		if (!otherShip.isPlayerShip)
		{
			var partXsWithIntent = Enumerable.Range(0, otherShip.parts.Count)
				.Where(x => otherShip.parts[x].intent is not null)
				.Select(x => x + otherShip.x)
				.ToList();

			foreach (var partXWithIntent in partXsWithIntent.Shuffle(args.State.rngActions).Take(wormAmount))
				args.Combat.Queue(new AStunPart { worldX = partXWithIntent });
		}

		args.Combat.Queue(new AStatus
		{
			targetPlayer = otherShip.isPlayerShip,
			status = (Status)ModEntry.Instance.Content.WormStatus.Id!.Value,
			statusAmount = -1
		});
	}
}
