using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class ThreadedSpikeCard : GuardianCard, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "ThreadedSpike", "Name"]).Localize,
		});
		
		helper.Content.Cards.RegisterCard($"{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::ExtraCard", new()
		{
			CardType = typeof(ExtraCard),
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "ThreadedSpike", "ExtraCard", "Name"]).Localize,
		});
		
		Abilities.SetAbilityType(entry.UniqueName, Abilities.MELEE_ABILITY);
		Abilities.SetBaseCooldown(entry.UniqueName, 6);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1, description = ModEntry.Instance.Localizations.Localize(["Card", "Ability", "ThreadedSpike", "Description", upgrade.ToString()]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new Action { PartCount = 5, Damage = 0 },
				new AAddCard { card = new ExtraCard(), destination = CardDestination.Hand },
			],
			Upgrade.A => [
				new Action { PartCount = 3, Damage = 1 },
				new AAddCard { card = new ExtraCard(), destination = CardDestination.Hand },
			],
			_ => [
				new Action { PartCount = 2, Damage = 1 },
				new AAddCard { card = new ExtraCard(), destination = CardDestination.Hand },
			],
		};

	private sealed class Action : CardAction
	{
		public required int PartCount;
		public int Damage = 1;

		public override List<Tooltip> GetTooltips(State s)
			=> Severed.GetTooltips().ToList();

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			if (PartCount <= 0)
				return;

			var targetWorldXs = Enumerable.Range(0, c.otherShip.parts.Count)
				.Select(i => (LocalX: i, Part: c.otherShip.parts[i]))
				.Where(e => e.Part.intent is IntentAttack)
				.Select(e => (LocalX: e.LocalX, WorldX: e.LocalX + c.otherShip.x, Part: e.Part, Intent: (IntentAttack)e.Part.intent!))
				.OrderByDescending(e => e.Intent.damage > 0)
				.ThenByDescending(e => e.WorldX >= s.ship.x && e.WorldX < s.ship.x + s.ship.parts.Count)
				.ThenByDescending(e => s.ship.GetPartAtWorldX(e.WorldX) is { } playerPart && playerPart.type != PType.empty)
				.ThenBy(_ => s.rngActions.NextInt())
				.Take(PartCount)
				.Select(e => e.WorldX)
				.ToList();

			if (targetWorldXs.Count <= 0)
				return;
			
			c.QueueImmediate(targetWorldXs.Select(worldX => new Single { WorldX = worldX, Damage = Damage }));
		}

		private sealed class Single : CardAction
		{
			public required int WorldX;
			public int Damage = 1;
			
			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);

				if (c.otherShip.GetPartAtWorldX(WorldX) is { } part)
					part.Severed = true;
				if (Damage > 0)
					c.otherShip.NormalDamage(s, c, Damage, WorldX);
			}
		}
	}

	private sealed class ExtraCard : GuardianCard, IHasCustomCardTraits
	{
		public override CardData GetData(State state)
		{
			var data = base.GetData(state) with { description = ModEntry.Instance.Localizations.Localize(["Card", "Ability", "ThreadedSpike", "ExtraCard", "Description", upgrade.ToString()]) };
			return upgrade switch
			{
				Upgrade.B => data with { cost = 1, temporary = true, infinite = true },
				_ => data with { cost = 1, temporary = true, singleUse = true },
			};
		}

		public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
			=> upgrade switch
			{
				Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Heavy.Trait, ModEntry.Instance.KokoroApi.Fleeting.Trait },
				_ => new HashSet<ICardTraitEntry>()
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.A => [
					new Action { Amount = 5 },
				],
				_ => [
					new Action { Amount = 3 },
				]
			};

		private sealed class Action : CardAction
		{
			public required int Amount;
			
			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				var cards = c.Abilities.OfType<ThreadedSpikeCard>().ToList();

				if (cards.Count <= 0)
				{
					timer = 0;
					return;
				}
				
				foreach (var card in cards)
					Abilities.SetCurrentCooldown(s, c, card, Abilities.GetCurrentCooldown(s, c, card) - Amount);
				Audio.Play(Event.Status_PowerUp);
			}
		}
	}
}