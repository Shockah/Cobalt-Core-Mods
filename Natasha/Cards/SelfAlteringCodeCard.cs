using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class SelfAlteringCodeCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SelfAlteringCode.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SelfAlteringCode", "name"]).Localize
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
			Upgrade.B => new() { cost = 1, description = ModEntry.Instance.Localizations.Localize(["card", "SelfAlteringCode", "description", upgrade.ToString()]) },
			_ => new() { cost = 1, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "SelfAlteringCode", "description", upgrade.ToString()]) },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new Action { Amount = 2 },
			],
			Upgrade.B => [
				new Action { Amount = 1, omitFromTooltips = true },
			],
			_ => [
				new Action { Amount = 1 },
			]
		};

	private sealed class Action : CardAction
	{
		public required int Amount;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var didAnything = false;
			foreach (var card in c.hand)
			{
				if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.KokoroApi.Limited.Trait))
					continue;

				ModEntry.Instance.KokoroApi.Limited.SetLimitedUses(s, card, ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, card) + Amount);
				didAnything = true;
			}

			if (didAnything)
				Audio.Play(Amount > 0 ? Event.Status_PowerUp : Event.Status_PowerDown);
		}
	}
}
