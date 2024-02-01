using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class DownsizeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Downsize.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Downsize", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.B ? 1 : 2,
			singleUse = upgrade != Upgrade.A,
			exhaust = upgrade == Upgrade.A,
			retain = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "Downsize", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		if (upgrade == Upgrade.B)
		{
			foreach (var card in c.hand)
				if (card.uuid != uuid && !card.GetDataWithOverrides(s).temporary)
					actions.Add(new ATemporarify { CardId = card.uuid });
		}
		else
		{
			var card = c.hand.LastOrDefault(card => card.uuid != uuid && !card.GetDataWithOverrides(s).temporary);
			if (card is not null)
				actions.Add(new ATemporarify { CardId = card.uuid });
		}
		actions.Add(new ATooltipAction
		{
			Tooltips = [
				new TTGlossary("cardtrait.temporary")
			]
		});
		return actions;
	}

	public sealed class ATemporarify : CardAction
	{
		public required int CardId;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			if (s.FindCard(CardId) is not { } card)
				return;
			card.temporaryOverride = true;
		}
	}
}
