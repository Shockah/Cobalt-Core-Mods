using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class OutburstCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Outburst.png"), StableSpr.cards_eunice).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Outburst", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 1,
				Upgrade.B => 0,
				_ => 2
			},
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(
				["card", "Outburst", "description", state.route is Combat ? "stateful" : "stateless", upgrade.ToString()],
				new { Damage = state.route is Combat combat ? combat.hand.Where(card => card.uuid != uuid).Sum(card => card.GetCurrentCost(state)) : 0 }
			)
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new Action { CardId = uuid, Exhaust = upgrade == Upgrade.B }
		];

	private sealed class Action : CardAction
	{
		public int? CardId;
		public bool Exhaust;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var sum = c.hand.Sum(card => card.GetCurrentCost(s));
			if (CardId is not { } cardId || s.FindCard(cardId) is not { } card)
				card = null;

			c.QueueImmediate([
				Exhaust ? ModEntry.Instance.KokoroApi.Actions.MakeExhaustEntireHandImmediate() : new ADiscard(),
				new AAttack { damage = GetActualDamage(s, sum, card: card) }
			]);
		}
	}
}
