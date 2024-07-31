using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class FocusCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Focus.png"), StableSpr.cards_BigShield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Focus", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			infinite = upgrade != Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = true,
				status = AuraManager.IntensifyStatus.Status,
				statusAmount = upgrade switch
				{
					Upgrade.A => 1,
					Upgrade.B => 2,
					_ => 1
				}
			},
			new AStatus
			{
				targetPlayer = true,
				status = AuraManager.VeilingStatus.Status,
				statusAmount = upgrade switch
				{
					Upgrade.A => 2,
					Upgrade.B => 3,
					_ => 1
				}
			}
		];
}
