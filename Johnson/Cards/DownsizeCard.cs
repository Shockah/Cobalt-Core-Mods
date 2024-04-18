using Nanoray.PluginManager;
using Nickel;
using System;
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
		=> [
			new ATemporarify
			{
				Cards = upgrade == Upgrade.B ? ATemporarify.CardsType.All : ATemporarify.CardsType.Right
			}
		];

	public sealed class ATemporarify : CardAction
	{
		public enum CardsType
		{
			Left, Right, All
		}

		public required CardsType Cards;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			switch (Cards)
			{
				case CardsType.Left:
					foreach (var card in c.hand)
					{
						if (card.GetDataWithOverrides(s).temporary)
							continue;
						ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, true, permanent: true);
						return;
					}
					break;
				case CardsType.Right:
					foreach (var card in ((IEnumerable<Card>)c.hand).Reverse())
					{
						if (card.GetDataWithOverrides(s).temporary)
							continue;
						ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, true, permanent: true);
						return;
					}
					break;
				case CardsType.All:
					foreach (var card in c.hand)
					{
						if (card.GetDataWithOverrides(s).temporary)
							continue;
						ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, true, permanent: true);
					}
					break;
			}
		}
	}
}
