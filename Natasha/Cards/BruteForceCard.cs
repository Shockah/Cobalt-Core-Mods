using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class BruteForceCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/BruteForce.png"), StableSpr.cards_MultiBlast).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BruteForce", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 4);
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 4);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait };

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, infinite = true, floppable = true },
			_ => new() { cost = 2 }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat).AsCardAction.Disabled(flipped),
				new AAttack { damage = GetDmg(s, ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1), xHint = 1, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.Limited.MakeVariableHint(uuid).AsCardAction.Disabled(!flipped),
				new AAttack { damage = GetDmg(s, ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, this)), xHint = 1, disabled = !flipped },
			],
			_ => [
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat).AsCardAction,
				new AAttack { damage = GetDmg(s, ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1), xHint = 1 },
				ModEntry.Instance.KokoroApi.Limited.MakeVariableHint(uuid).AsCardAction,
				new AAttack { damage = GetDmg(s, ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, this)), xHint = 1 },
			]
		};
}
