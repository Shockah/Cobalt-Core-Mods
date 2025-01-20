using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class NeglectSafetyCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/NeglectSafety.png"), StableSpr.cards_eunice).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "NeglectSafety", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade.Switch<HashSet<ICardTraitEntry>>(
			() => [ModEntry.Instance.KokoroApi.Finite.Trait],
			() => [ModEntry.Instance.KokoroApi.Finite.Trait],
			() => []
		);

	public override CardData GetData(State state)
	{
		return upgrade.Switch<CardData>(
			() => new() { cost = 0, floppable = true, description = ModEntry.Instance.Localizations.Localize(["card", "NeglectSafety", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]) },
			() => new() { cost = 0, description = ModEntry.Instance.Localizations.Localize(["card", "NeglectSafety", "description", upgrade.ToString()]) },
			() => new() { cost = 0, floppable = true, infinite = true, description = ModEntry.Instance.Localizations.Localize(["card", "NeglectSafety", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]) }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new RemoveAnalyzedAction { CardId = uuid, disabled = flipped },
				new ADrawCard { count = 1, disabled = !flipped },
				new AHurt { targetPlayer = true, hurtAmount = 1, hurtShieldsFirst = true, omitFromTooltips = true },
			],
			a: () => [
				new RemoveAnalyzedAction { CardId = uuid },
				new ADrawCard { count = 1 },
				new AHurt { targetPlayer = true, hurtAmount = 1, hurtShieldsFirst = true, omitFromTooltips = true },
			],
			b: () => [
				new RemoveAnalyzedAction { CardId = uuid, disabled = flipped },
				new ADrawCard { count = 1, disabled = !flipped },
				new AHurt { targetPlayer = true, hurtAmount = 1, hurtShieldsFirst = true, omitFromTooltips = true },
			]
		);

	private sealed class RemoveAnalyzedAction : CardAction
	{
		public required int CardId;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (s.FindCard(CardId) is not { } card)
			{
				timer = 0;
				return;
			}
			
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, false, permanent: false);
		}
	}
}
