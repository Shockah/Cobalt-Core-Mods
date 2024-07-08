using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class RestartEnginesCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/RestartEngines.png"), StableSpr.cards_Ace).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RestartEngines", "name"]).Localize
		});

		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.None, 2);
		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.A, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [],
			_ => [Limited.Trait],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 0 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = false, status = Status.lockdown, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			new ADrawCard { count = 1 },
		];
}
