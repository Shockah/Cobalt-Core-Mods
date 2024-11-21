using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class SpywareCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Spyware.png"), StableSpr.cards_Shield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Spyware", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 4);
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait };

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Limited.MakeVariableHint(uuid).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, this), xHint = 1 },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 2 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.Limited.MakeVariableHint(uuid).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, this), xHint = 1 },
			]
		};
}
