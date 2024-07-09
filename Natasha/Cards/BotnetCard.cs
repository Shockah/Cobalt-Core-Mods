using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class BotnetCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Botnet.png"), StableSpr.cards_PowerPlay).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Botnet", "name"]).Localize
		});

		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.A, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [],
			_ => [Limited.Trait],
		});

	public override CardData GetData(State state)
		=> new() { cost = 2 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new StepAction { CardId = uuid, Step = 2, Steps = 2, Action = new AStatus { targetPlayer = true, status = Status.powerdrive, statusAmount = 1 } },
			],
			_ => [
				new StepAction { CardId = uuid, Step = 3, Steps = 3, Action = new AStatus { targetPlayer = true, status = Status.powerdrive, statusAmount = 1 } },
			]
		};
}
