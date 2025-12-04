using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class MissileSalvoCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/MissileSalvo.png"), StableSpr.cards_SeekerMissileCard).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MissileSalvo", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 3);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 3);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 3 },
			_ => new() { cost = 1 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry>(),
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new Missile { missileType = MissileType.breacher, targetPlayer = false }.SetWobbly() }.SetStacked(),
				new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false }.SetWobbly() }.SetStacked(),
				new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false }.SetWobbly() }.SetStacked(),
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 1 },
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 1, 3,
					new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false } }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 2, 3,
					new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false } }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 3, 3,
					new ASpawn { thing = new Missile { missileType = MissileType.breacher, targetPlayer = false } }
				).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 1, 3,
					new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false } }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 2, 3,
					new ASpawn { thing = new Missile { missileType = MissileType.normal, targetPlayer = false } }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 3, 3,
					new ASpawn { thing = new Missile { missileType = MissileType.breacher, targetPlayer = false } }
				).AsCardAction,
			],
		};
}