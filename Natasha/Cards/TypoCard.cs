using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class TypoCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Typo.png"), StableSpr.cards_Overdrive).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Typo", "name"]).Localize
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
		=> new() { cost = 0, infinite = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2, new AStatus { targetPlayer = false, status = Status.powerdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AStatus { targetPlayer = true, status = Status.powerdrive, statusAmount = 1 }).AsCardAction,
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 3, new AStatus { targetPlayer = false, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 3, new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 3, 3, new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 3, 3, new DiscardSelfAction { CardId = uuid }).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2, new AStatus { targetPlayer = false, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new DiscardSelfAction { CardId = uuid }).AsCardAction,
			]
		};

	private sealed class DiscardSelfAction : CardAction
	{
		public required int CardId;

		public override Icon? GetIcon(State s)
			=> new Icon { path = StableSpr.icons_discardCard };

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					Icon = StableSpr.icons_discardCard,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["card", "Typo", "discardSelfAction", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["card", "Typo", "discardSelfAction", "description"])
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var index = c.hand.FindIndex(card => card.uuid == CardId);
			if (index == -1)
			{
				timer = 0;
				return;
			}

			var card = c.hand[index];
			c.hand.RemoveAt(index);
			card.waitBeforeMoving = 0;
			card.OnDiscard(s, c);
			c.SendCardToDiscard(s, card);
			Audio.Play(Event.CardHandling);
		}
	}
}
