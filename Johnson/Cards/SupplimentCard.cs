using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class SupplimentCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Suppliment.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Suppliment", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			description = ModEntry.Instance.Localizations.Localize(["card", "Suppliment", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [
			new AStatus
			{
				targetPlayer = true,
				status = Status.shield,
				statusAmount = upgrade == Upgrade.A ? 2 : 1
			},
			new ADiscount
			{
				CardId = c.hand.LastOrDefault(card => card.uuid != uuid)?.uuid
			}
		];
		if (upgrade == Upgrade.B)
			actions.Add(new ADiscount
			{
				CardId = c.hand.FirstOrDefault(card => card.uuid != uuid)?.uuid
			});
		return actions;
	}

	public sealed class ADiscount : CardAction
	{
		public required int? CardId;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			if (CardId is not { } cardId || s.FindCard(cardId) is not { } card)
				return;
			card.discount--;
		}

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTGlossary("cardtrait.discount", 1)
			];
	}
}
