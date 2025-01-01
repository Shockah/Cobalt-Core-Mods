using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class AbyssalPowerEvent : IRegisterable
{
	private static string EventName = null!;
	private static ICardEntry AbyssalPowerCardEntry = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AbyssalPowerCard.RegisterCard(helper);

		EventName = $"{package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}";

		DB.story.all[EventName] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = true,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "tentacle",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "1-Tentacle"])
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.dizzy.Key(),
							loopTag = "intense",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = Deck.riggs.Key(),
							loopTag = "gun",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = Deck.peri.Key(),
							loopTag = "panic",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = Deck.goat.Key(),
							loopTag = "panic",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = Deck.eunice.Key(),
							loopTag = "panic",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = Deck.hacker.Key(),
							loopTag = "intense",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = "comp",
							loopTag = "worried",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
						new CustomSay
						{
							who = "crew",
							loopTag = "gameover",
							Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "2-Any"])
						},
					]
				},
				new CustomSay
				{
					who = "tentacle",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "3-Tentacle"])
				},
				new CustomSay
				{
					who = "crew",
					loopTag = "neutral",
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "4-Any"])
				},
				new CustomSay
				{
					who = "tentacle",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "5-Tentacle"])
				},
				new CustomSay
				{
					who = "comp",
					loopTag = "squint",
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "6-CAT"])
				},
			],
			choiceFunc = EventName
		};
		DB.story.all[$"{EventName}::Yes"] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = false,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "comp",
					loopTag = "neutral",
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "Yes-1-CAT"])
				},
			]
		};
		DB.story.all[$"{EventName}::No"] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = false,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "tentacle",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "No-1-Tentacle"])
				},
				new CustomSay
				{
					who = "comp",
					loopTag = "squint",
					Text = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "No-2-CAT"])
				},
			],
			choiceFunc = $"{EventName}::EnterCombat"
		};

		DB.eventChoiceFns[EventName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
		DB.eventChoiceFns[$"{EventName}::EnterCombat"] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetEnterCombatChoices));
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		var node = DB.story.all[EventName];
		node.never = settings.DisabledEvents.Contains(MoreEvent.AbyssalPower) ? true : null;
		node.dontCountForProgression = settings.DisabledEvents.Contains(MoreEvent.AbyssalPower);
		AbyssalPowerCardEntry.Configuration.Meta.unreleased = settings.DisabledEvents.Contains(MoreEvent.AbyssalPower);
	}

	[UsedImplicitly]
	private static List<Choice> GetChoices(State state)
		=> [
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "Choice-Yes"]),
				key = $"{EventName}::Yes",
				actions = [
					new AAddCard
					{
						destination = CardDestination.Deck,
						card = new AbyssalPowerCard()
					},
					new ATooltipAction
					{
						Tooltips = [new TTCard { card = new AbyssalVisions() }]
					}
				]
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "Choice-No"]),
				key = $"{EventName}::No"
			}
		];

	private static List<Choice> GetEnterCombatChoices(State state)
	{
		List<CardAction> rewardActions = [];
		if (state.ship.hpGainFromEliteKills != 0)
			rewardActions.Add(
				new AShipUpgrades
				{
					actions = [
						new AHullMax
						{
							amount = state.ship.hpGainFromEliteKills,
							targetPlayer = true
						},
						new AHeal
						{
							healAmount = state.ship.hpGainFromEliteKills,
							targetPlayer = true
						}
					]
				}
			);

		rewardActions.AddRange([
			new ACardOffering
			{
				amount = 3,
				battleType = BattleType.Elite
			},
			new AArtifactOffering
			{
				amount = state.GetDifficulty() >= 2 ? 2 : 3,
				limitPools = [ArtifactPool.Common]
			},
		]);

		return [
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "AbyssalPower", "No-Choice-EnterCombat"]),
				actions = [
					new ADelayToRewards { Actions = rewardActions },
					new AStartCombat { ai = new AsteroidBoss() }
				]
			}
		];
	}

	private sealed class AbyssalPowerCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			AbyssalPowerCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.corrupted,
					rarity = Rarity.common,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "AbyssalPower", "card", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 0,
				floppable = true,
				exhaust = upgrade == Upgrade.B,
				art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new AAttack
					{
						damage = GetDmg(s, 4),
						disabled = flipped,
					},
					new ADummyAction(),
					new AStatus
					{
						targetPlayer = true,
						status = Status.overdrive,
						statusAmount = 2,
						disabled = !flipped,
					}
				],
				_ => [
					new AAttack
					{
						damage = GetDmg(s, 4),
						disabled = flipped,
					},
					new AAddCard
					{
						destination = CardDestination.Hand,
						card = new AbyssalVisions { discount = upgrade == Upgrade.A ? -1 : 0 },
						disabled = flipped,
					},
					new ADummyAction(),
					new AStatus
					{
						targetPlayer = true,
						status = Status.overdrive,
						statusAmount = 2,
						disabled = !flipped,
					},
					new AAddCard
					{
						destination = CardDestination.Hand,
						card = new AbyssalVisions { discount = upgrade == Upgrade.A ? -1 : 0 },
						disabled = !flipped,
					}
				]
			};
	}
}
