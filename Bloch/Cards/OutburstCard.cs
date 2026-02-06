using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class OutburstCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Outburst.png"), StableSpr.cards_FumeCannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Outburst", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 2 },
			_ => new() { cost = 3 },
		};

	private bool WillStayInHandOnPlay(State s, Combat c)
	{
		if (!c.hand.Contains(this))
			return false;
		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, this, ModEntry.Instance.Helper.Content.Cards.InfiniteCardTrait))
			return true;
		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, this, ModEntry.Instance.KokoroApi.Finite.Trait) && ModEntry.Instance.KokoroApi.Finite.GetFiniteUses(s, this) > 1)
			return true;
		return false;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		switch (upgrade)
		{
			case Upgrade.B:
			{
				var extraAmount = c.hand.Count - (WillStayInHandOnPlay(s, c) ? 0 : 1);
				return
				[
					new ADiscard(),
					new DiscardedVariableHint { ExtraAmount = extraAmount },
					new AAttack { damage = GetDmg(s, c.DiscardedCardCount + extraAmount), xHint = 1 },
				];
			}
			default:
				return
				[
					new DiscardedVariableHint(),
					new AAttack { damage = GetDmg(s, c.DiscardedCardCount), xHint = 1 },
				];
		}
	}
}
