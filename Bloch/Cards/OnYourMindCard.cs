using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;
using Shockah.Kokoro;

namespace Shockah.Bloch;

internal sealed class OnYourMindCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/OnYourMind.png"), StableSpr.cards_Corrode).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "OnYourMind", "name"]).Localize
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 3);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 3);
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry>(),
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ScryAction { Amount = 1 },
				new ScryAction { Amount = 2 },
				new ScryAction { Amount = 3 },
				new ADrawCard { count = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 1 },
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn).AsCardAction,
				new ScryAction { Amount = ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn) + 1, xHint = 1 },
				new ADrawCard { count = 1 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn).AsCardAction,
				new ScryAction { Amount = ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn) + 1, xHint = 1 },
				new ADrawCard { count = 1 },
			],
		};
}
