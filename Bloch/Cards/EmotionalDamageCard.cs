using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class EmotionalDamageCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/EmotionalDamage.png"), StableSpr.cards_DrakeCannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "EmotionalDamage", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 1 : 2
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new SpontaneousManager.TriggerAction
			{
				Action = new AAttack
				{
					damage = GetDmg(s, upgrade == Upgrade.B ? 4 : 1),
					stunEnemy = true
				}
			},
			new AAttack
			{
				damage = GetDmg(s, 0),
				stunEnemy = true
			}
		];
}
