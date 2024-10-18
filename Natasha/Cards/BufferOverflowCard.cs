using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class BufferOverflowCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/BufferOverflow.png"), StableSpr.cards_HandCannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BufferOverflow", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 4);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Limited.Trait };

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid).AsCardAction,
				new AAttack { damage = GetDmg(s, (ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this) + 1) * 2), xHint = 2 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid).AsCardAction,
				new AAttack { damage = GetDmg(s, ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this) + 1), xHint = 1 },
			]
		};
}
