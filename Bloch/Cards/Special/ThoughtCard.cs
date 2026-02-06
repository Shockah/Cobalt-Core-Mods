using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class ThoughtCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(typeof(UntangleCard)),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Thought.png"), StableSpr.cards_CloudSave).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Thought", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, temporary = true, exhaust = true },
			Upgrade.A => new() { cost = 0, temporary = true, exhaust = true, retain = true },
			_ => new() { cost = 0, temporary = true, exhaust = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait },
			_ => new HashSet<ICardTraitEntry>(),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.Api.MakeChooseAura(card: this, amount: 2, actionId: 1),
				ModEntry.Instance.Api.MakeChooseAura(card: this, amount: 2, actionId: 2),
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
			],
			_ => [
				ModEntry.Instance.Api.MakeChooseAura(card: this, amount: 1),
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
			],
		};
}
