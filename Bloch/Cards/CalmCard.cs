using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class CalmCard : Card, IRegisterable
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
			Art = StableSpr.cards_ShieldSurge,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Calm.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Calm", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new OncePerTurnManager.TriggerAction
			{
				Action = new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = upgrade == Upgrade.A ? 2 : 1
				},
				disabled = ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, this, OncePerTurnManager.OncePerTurnTriggeredTrait)
			},
			new ADrawCard
			{
				count = upgrade == Upgrade.B ? 2 : 1
			}
		];
}
