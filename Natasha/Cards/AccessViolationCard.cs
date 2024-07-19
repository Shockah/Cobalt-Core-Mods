using daisyowl.text;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class AccessViolationCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/AccessViolation.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AccessViolation", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 2);

		ModEntry.Instance.KokoroApi.RegisterCardRenderHook(new Hook(), 0);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 3, description = ModEntry.Instance.Localizations.Localize(["card", "AccessViolation", "description"]) },
			Upgrade.A => new() { cost = 2, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "AccessViolation", "description"]) },
			_ => new() { cost = 3, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "AccessViolation", "description"]) },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new Action { ExtraUses = 1 }];

	private sealed class Action : CardAction
	{
		public int ExtraUses;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. Limited.Trait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var index = 0;
			foreach (var card in c.exhausted.ToList())
			{
				if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Limited.Trait))
					continue;

				c.exhausted.Remove(card);
				card.waitBeforeMoving = index++ * 0.05;
				c.SendCardToDiscard(s, card);

				if (ExtraUses != 0)
					card.SetLimitedUses(card.GetLimitedUses(s) + ExtraUses);
			}

			if (index != 0)
			{
				Audio.Play(Event.CardHandling);
				if (ExtraUses != 0)
					Audio.Play(ExtraUses > 0 ? Event.Status_PowerUp : Event.Status_PowerDown);
			}
		}
	}

	private sealed class Hook : ICardRenderHook
	{
		public Font? ReplaceTextCardFont(G g, Card card)
		{
			if (card is not AccessViolationCard)
				return null;
			return ModEntry.Instance.KokoroApi.PinchCompactFont;
		}
	}
}
