using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class SendRegardsCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SendRegards.png"), StableSpr.cards_SeekerMissileCard).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SendRegards", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 1, 2,
					new ASpawn { thing = new Missile { missileType = MissileType.heavy, targetPlayer = false } }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 2, 2,
					new ASpawn { thing = new Asteroid() }
				).AsCardAction,
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new Missile { missileType = MissileType.heavy, targetPlayer = false } },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false } },
			],
		};
}