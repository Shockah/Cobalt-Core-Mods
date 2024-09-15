using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class BufferCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Buffer.png"), StableSpr.cards_ExtraBattery).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Buffer", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 1);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 3);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 1);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Limited.Trait };

	public override CardData GetData(State state)
		=> new() { cost = 0 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Actions.MakeOnTurnEndAction(
					new ChangeLimitedUsesAction { CardId = uuid, Amount = 1 }
				),
				new AEnergy { changeAmount = 2 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.Actions.MakeOnTurnEndAction(
					new ChangeLimitedUsesAction { CardId = uuid, Amount = 1 }
				),
				new AEnergy { changeAmount = 1 },
			]
		};
}
