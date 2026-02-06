using System;
using System.Collections.Generic;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class DigressionCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Digression.png"), StableSpr.cards_CloudSave).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Digression", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Digression", "description"]);
		return upgrade switch
		{
			Upgrade.B => new() { description = description, cost = 1 },
			Upgrade.A => new() { description = description, cost = 0, exhaust = true, retain = true },
			_ => new() { description = description, cost = 0, exhaust = true },
		};
	}

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
		var amount = Math.Max(c.hand.Count - (WillStayInHandOnPlay(s, c) ? 0 : 1), 0);
		return [
			new RecycleAction(),
			new ScryAction { Amount = amount },
			new ADrawCard { count = amount },
		];
	}

	private sealed class RecycleAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (c.hand.Count == 0)
			{
				timer = 0;
				return;
			}
			
			Audio.Play(Event.CardHandling);

			for (var i = c.hand.Count - 1; i >= 0; i--)
			{
				var card = c.hand[i];
				card.waitBeforeMoving = i * 0.05;
				card.OnDiscard(s, c);
				s.SendCardToDeck(card);
			}
		}
	}
}
