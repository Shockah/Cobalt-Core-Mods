using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class OnionRoutingCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/OnionRouting.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "OnionRouting", "name"]).Localize
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.B, 3);
	}
	
	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0 },
			Upgrade.B => new() { cost = 0, recycle = true },
			_ => new() { cost = 0 }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 2 },
			ModEntry.Instance.KokoroApi.Sequence.MakeAction(
				uuid,
				IKokoroApi.IV2.ISequenceApi.Interval.Combat,
				upgrade == Upgrade.A ? 2 : 1, upgrade == Upgrade.A ? 3 : 2,
				new AStatus { targetPlayer = true, status = Status.drawLessNextTurn, statusAmount = 1 }
			).AsCardAction,
			ModEntry.Instance.KokoroApi.Sequence.MakeAction(
				uuid,
				IKokoroApi.IV2.ISequenceApi.Interval.Combat,
				upgrade == Upgrade.A ? 3 : 2, upgrade == Upgrade.A ? 3 : 2,
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 }
			).AsCardAction,
		];
}
