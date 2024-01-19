using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class HeartbreakCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Heartbreak", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_WeakenHull,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Heartbreak", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade != Upgrade.None ? 1 : 2,
			exhaust = true,
			buoyant = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				},
				new AAttack
				{
					damage = GetDmg(s, 0),
					weaken = true
				},
				new AAttack
				{
					damage = GetDmg(s, 2),
					piercing = true,
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				}
			],
			_ => [
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				},
				new AAttack
				{
					damage = GetDmg(s, 2),
					piercing = true,
					weaken = true
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				}
			]
		};
}
