using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
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
			cost = upgrade == Upgrade.B ? 0 : 2,
			singleUse = upgrade != Upgrade.A,
			exhaust = upgrade == Upgrade.A,
			retain = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "Downsize", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ATemporarify { Cards = upgrade == Upgrade.B ? ATemporarify.CardsType.All : ATemporarify.CardsType.Right },
			new ADummyAction { dialogueSelector = $".Played::{Key()}" },
		];

	public sealed class ATemporarify : CardAction
	{
		public enum CardsType
		{
			Left, Right, All
		}

		public required CardsType Cards;

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("cardtrait.temporary")];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (c.hand.Count == 0)
			{
				timer = 0;
				return;
			}

			var didAnything = false;

			switch (Cards)
			{
				case CardsType.Left:
					TryTemporarify(c.hand[0]);
					break;
				case CardsType.Right:
					TryTemporarify(c.hand[^1]);
					break;
				case CardsType.All:
					foreach (var card in c.hand)
						TryTemporarify(card);
					break;
			}

			if (!didAnything)
			{
				timer = 0;
				return;
			}

			Audio.Play(Event.CardHandling);

			void TryTemporarify(Card card)
			{
				var data = card.GetDataWithOverrides(s);
				if (data.temporary || data.unremovableAtShops)
					return;

				didAnything = true;
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, true, permanent: true);
			}
		}
	}
}
