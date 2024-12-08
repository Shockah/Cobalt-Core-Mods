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
		
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [ModEntry.Instance.KokoroApi.Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 3, description = ModEntry.Instance.Localizations.Localize(["card", "AccessViolation", "description", upgrade.ToString()]) },
			Upgrade.A => new() { cost = 3, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "AccessViolation", "description", upgrade.ToString()]) },
			_ => new() { cost = 3, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "AccessViolation", "description", upgrade.ToString()]) },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new Action(),
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 },
			],
			_ => [
				new Action(),
			]
		};

	private sealed class Action : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTGlossary("action.bypass"),
				.. ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(s, null) ?? []
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			c.QueueImmediate(
				c.exhausted
					.Where(card => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.KokoroApi.Limited.Trait))
					.Select(card => ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeAction(card).AsCardAction)
			);
		}
	}
}
