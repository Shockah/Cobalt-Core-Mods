using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class PingCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ping.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Ping", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [ModEntry.Instance.KokoroApi.Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2) },
				new ADrawCard { count = 2 },
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, 1, 2, new AAttack { damage = GetDmg(s, 3) }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, 2, 2, new ADrawCard { count = 3 }).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, 1, 2, new AAttack { damage = GetDmg(s, 2) }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, 2, 2, new ADrawCard { count = 2 }).AsCardAction,
			]
		};
}
