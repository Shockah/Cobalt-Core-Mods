using System.Collections.Generic;
using System.Linq;
using Nickel;

namespace Shockah.Soggins;

public sealed class AAddApologyCard : CardAction
{
	public int Amount = 1;
	public CardDestination Destination = CardDestination.Hand;

	public override Icon? GetIcon(State s)
		=> new((Spr)ModEntry.Instance.GainApologySprite.Id!.Value, Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::AAddApologyCard")
			{
				Icon = (Spr)ModEntry.Instance.GainApologySprite.Id!.Value,
				TitleColor = Colors.action,
				Title = I18n.GainApologiesActionName,
				Description = I18n.GainApologiesActionText,
				vals = [Amount],
			},
			new TTCard { card = new RandomPlaceholderApologyCard() },
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var cards = Enumerable.Range(0, Amount)
			.Select(_ => SmugStatusManager.GenerateAndTrackApology(s, c, s.rngActions))
			.Reverse();

		foreach (var card in cards)
			c.QueueImmediate(new AAddCard
			{
				card = card,
				destination = Destination
			});
	}
}
