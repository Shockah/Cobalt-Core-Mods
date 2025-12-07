using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class RevampCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Revamp.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Revamp", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.A ? 0 : 1,
			description = ModEntry.Instance.Localizations.Localize(["card", "Revamp", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [new Action { Choose = true }],
			_ => [new Action { Choose = false }],
		};

	private sealed class Action : CardAction
	{
		public bool Choose;

		public override List<Tooltip> GetTooltips(State s)
			=> [ModEntry.Instance.KokoroApi.TemporaryUpgrades.UpgradeTooltip];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (c.hand.Count == 0)
			{
				timer = 0;
				return;
			}

			var didAnything = false;
			TryUpgrade(c.hand[0], Upgrade.A);
			TryUpgrade(c.hand[^1], Upgrade.B);

			if (!didAnything)
			{
				timer = 0;
				return;
			}

			Audio.Play(Event.CardHandling);

			void TryUpgrade(Card card, Upgrade upgrade)
			{
				if (card.upgrade != Upgrade.None)
					return;
				if (!card.GetMeta().upgradesTo.Contains(upgrade))
					return;

				if (Choose)
				{
					c.QueueImmediate(ModEntry.Instance.KokoroApi.TemporaryUpgrades.MakeChooseTemporaryUpgradeAction(card.uuid).SetAllowCancel(false).AsCardAction);
				}
				else
				{
					didAnything = true;
					ModEntry.Instance.KokoroApi.TemporaryUpgrades.SetTemporaryUpgrade(s, card, upgrade);
				}
			}
		}
	}
}