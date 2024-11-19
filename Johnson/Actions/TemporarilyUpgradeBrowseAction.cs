using System;
using System.Collections.Generic;

namespace Shockah.Johnson;

public sealed class TemporarilyUpgradeBrowseAction : CardAction
{
	public int Discount = 0;
	public int Strengthen = 0;

	public override List<Tooltip> GetTooltips(State s)
		=> [
			.. Discount == 0 ? Array.Empty<Tooltip>() : [new TTGlossary("cardtrait.discount", Discount)],
			.. Strengthen == 0 ? Array.Empty<Tooltip>() : [ModEntry.Instance.Api.GetStrengthenTooltip(Strengthen)],
			.. ModEntry.Instance.KokoroApi.TemporaryUpgrades.CardTrait.Configuration.Tooltips?.Invoke(s, null) ?? []
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (selectedCard is null)
		{
			timer = 0;
			return;
		}
		
		selectedCard.discount += Discount;
		selectedCard.AddStrengthen(Strengthen);
		
		var action = ModEntry.Instance.KokoroApi.TemporaryUpgrades.MakeChooseTemporaryUpgradeAction(selectedCard.uuid).AsCardAction;
		action.timer = 0;
		c.QueueImmediate(action);
	}
}