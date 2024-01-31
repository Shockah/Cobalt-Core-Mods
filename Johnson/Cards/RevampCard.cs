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
	{
		List<CardAction> actions = [];
		var leftCard = c.hand.FirstOrDefault(card => card.uuid != uuid && card.IsUpgradable());
		var rightCard = c.hand.LastOrDefault(card => card.uuid != uuid && card.IsUpgradable());

		if (upgrade == Upgrade.B)
		{
			if (leftCard is not null)
				actions.Add(new ATemporarilyUpgrade
				{
					CardId = leftCard.uuid
				});
			if (rightCard is not null && leftCard != rightCard)
				actions.Add(new ATemporarilyUpgrade
				{
					CardId = rightCard.uuid
				});
		}
		else
		{
			if (leftCard is not null)
				actions.Add(new ATemporarilySpecificUpgrade
				{
					CardId = leftCard.uuid,
					Upgrade = Upgrade.A
				});
			if (rightCard is not null && leftCard != rightCard)
				actions.Add(new ATemporarilySpecificUpgrade
				{
					CardId = rightCard.uuid,
					Upgrade = Upgrade.B
				});
		}
		return actions;
	}

	public sealed class ATemporarilySpecificUpgrade : CardAction
	{
		public required int CardId;
		public required Upgrade Upgrade;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			if (s.FindCard(CardId) is not { } card)
				return;
			card.SetTemporarilyUpgraded(true);
			card.upgrade = Upgrade;
		}
	}
}
