using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Shockah.MORE;

internal sealed class ToothCards : IRegisterable
{
	internal static ICardEntry FiddleCardEntry = null!;
	internal static ICardEntry SlipCardEntry = null!;
	internal static ICardEntry FinalFormCardEntry = null!;
	internal static ICardEntry SkimCardEntry = null!;
	internal static ICardEntry SmashCardEntry = null!;

	internal static string[] AllToothCardKeys = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		FiddleCard.RegisterCard(helper);
		SlipCard.RegisterCard(helper);
		FinalFormCard.RegisterCard(helper);
		SkimCard.RegisterCard(helper);
		SmashCard.RegisterCard(helper);

		AllToothCardKeys = [
			nameof(Buckshot), nameof(WaltzCard), nameof(BruiseCard), nameof(LightningBottle),
			FiddleCardEntry.UniqueName, SlipCardEntry.UniqueName, FinalFormCardEntry.UniqueName, SkimCardEntry.UniqueName, SmashCardEntry.UniqueName,
		];

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(MapEvent), nameof(MapEvent.MakeRoute)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapEvent_MakeRoute_Prefix)))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.ToothCardOffering)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Events_ToothCardOffering_Postfix)))
		);
	}

	private static void MapEvent_MakeRoute_Prefix(State s)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(s, "ToothChoicesPage");

	private static void Events_ToothCardOffering_Postfix(State s, out List<Choice> __result)
	{
		var rand = new Rand(s.rngCurrentEvent.seed + 4682101);
		var presentedChoices = AllToothCardKeys
			.Where(key => !ModEntry.Instance.Settings.ProfileBased.Current.DisabledToothCards.Contains(key))
			.Shuffle(rand)
			.Skip(Math.Clamp(s.GetDifficulty(), 0, Math.Max(AllToothCardKeys.Length - (s.GetHardEvents() ? 3 : 4), 0)))
			.ToList();

		var needsPaging = presentedChoices.Count >= 5;
		var perPage = needsPaging ? 4 : presentedChoices.Count;
		var pageCount = (int)Math.Ceiling(1.0 * presentedChoices.Count / perPage);
		var page = needsPaging ? (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(s, "ToothChoicesPage") % pageCount) : 0;
		var pageStartIndex = page * perPage;

		var choices = new List<Choice>();

		for (var i = 0; i < perPage; i++)
		{
			if (presentedChoices.Count <= pageStartIndex + i)
				break;
			if (!DB.cards.TryGetValue(presentedChoices[pageStartIndex + i], out var cardType))
				continue;

			var card = (Card)Activator.CreateInstance(cardType)!;
			choices.Add(new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "choices", i.ToString()], new { Card = card.GetLocName() }),
				key = "ToothCardOffering_After",
				actions = [new AAddCard { card = card, callItTheDeckNotTheDrawPile = true }],
			});
		}

		if (needsPaging)
			choices.Add(new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "nextPageChoice"]),
				key = "ToothCardOffering",
				actions = [new NextPageAction()],
			});

		choices.Add(new Choice
		{
			label = Loc.T("ToothCardOffering_No", "Yeah, no."),
			key = "ToothCardOffering_After",
		});

		__result = choices;
	}

	private sealed class NextPageAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			ModEntry.Instance.Helper.ModData.SetModData(s, "ToothChoicesPage", ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(s, "ToothChoicesPage") + 1);
			s.GetCurrentQueue().Queue(new ASkipDialogue());
		}
	}

	[UsedImplicitly]
	private sealed class FiddleCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			FiddleCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Fiddle", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 0,
				art = StableSpr.cards_riggs,
				description = ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fiddle", "description", upgrade.ToString()]),
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new ACardSelect
					{
						browseSource = CardBrowse.Source.Hand,
						browseAction = new DiscardAndDrawAction()
					}
				],
				Upgrade.A => [
					new ACardSelect
					{
						browseSource = CardBrowse.Source.Hand,
						browseAction = new RedrawAction { Times = 2 }
					}
				],
				_ => [
					new ACardSelect
					{
						browseSource = CardBrowse.Source.Hand,
						browseAction = new RedrawAction()
					}
				]
			};

		private sealed class RedrawAction : CardAction
		{
			public int Times = 1;

			public override string GetCardSelectText(State s)
				=> ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fiddle", "drawACardInYourHand"]);

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				if (selectedCard is null || s.FindCard(selectedCard.uuid) is not { } card)
				{
					timer = 0;
					return;
				}

				s.RemoveCardFromWhereverItIs(selectedCard.uuid);
				c.SendCardToHand(s, card);
				Audio.Play(Event.CardHandling);

				if (Times > 1)
					c.QueueImmediate(new RedrawAction { selectedCard = selectedCard, Times = Times - 1 });
			}
		}

		private sealed class DiscardAndDrawAction : CardAction
		{
			public override string GetCardSelectText(State s)
				=> ModEntry.Instance.Localizations.Localize(["event", "ToothCardOffering", "card", "Fiddle", "discardAndDrawACardInYourHand"]);

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				if (selectedCard is null || s.FindCard(selectedCard.uuid) is not { } card)
				{
					timer = 0;
					return;
				}

				s.RemoveCardFromWhereverItIs(card.uuid);
				c.SendCardToDiscard(s, card);
				Audio.Play(Event.CardHandling);

				c.QueueImmediate(new DrawBackAction { selectedCard = selectedCard });
			}

			private sealed class DrawBackAction : CardAction
			{
				public override void Begin(G g, State s, Combat c)
				{
					base.Begin(g, s, c);
					if (selectedCard is null || s.FindCard(selectedCard.uuid) is not { } card)
					{
						timer = 0;
						return;
					}

					s.RemoveCardFromWhereverItIs(card.uuid);
					c.SendCardToHand(s, card);
					Audio.Play(Event.CardHandling);
				}
			}
		}
	}

	[UsedImplicitly]
	private sealed class SlipCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			SlipCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Slip", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 0,
				flippable = true,
				art = StableSpr.cards_Dodge,
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new AStatus { targetPlayer = true, status = Status.engineStall, statusAmount = 3 },
					new AMove { targetPlayer = true, dir = 1 },
					new AMove { targetPlayer = true, dir = 1 },
					new AMove { targetPlayer = true, dir = 1 },
				],
				Upgrade.A => [
					new AMove { targetPlayer = true, dir = 0 },
					new AMove { targetPlayer = true, dir = 0 },
				],
				_ => [
					new AMove { targetPlayer = true, dir = 0 },
				]
			};
	}

	[UsedImplicitly]
	private sealed class FinalFormCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			FinalFormCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "FinalForm", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 1,
				art = StableSpr.cards_Overpower,
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 9 },
					new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.temporaryCheap, statusAmount = 1 },
					new AEndTurn(),
				],
				Upgrade.A => [
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 9 },
					new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.autopilot, statusAmount = 1 },
					new AEndTurn(),
				],
				_ => [
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 9 },
					new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
					new AEndTurn(),
				]
			};
	}

	[UsedImplicitly]
	private sealed class SkimCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			SkimCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Skim", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 1,
				art = StableSpr.cards_Options,
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new ADrawCard { count = 5 },
					new ADiscard { count = 5 },
				],
				Upgrade.A => [
					new ADrawCard { count = 10 },
					new ADiscard(),
				],
				_ => [
					new ADrawCard { count = 5 },
					new ADiscard(),
				]
			};
	}

	[UsedImplicitly]
	private sealed class SmashCard : Card
	{
		public static void RegisterCard(IModHelper helper)
		{
			SmashCardEntry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					deck = Deck.tooth,
					rarity = Rarity.rare,
					upgradesTo = [Upgrade.A, Upgrade.B],
					dontOffer = true,
					extraGlossary = ["cardMisc.toothCardExtraTooltip"],
				},
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ToothCardOffering", "card", "Smash", "name"]).Localize
			});
		}

		public override CardData GetData(State state)
			=> new()
			{
				cost = 1,
				art = StableSpr.cards_GoatDrone,
			};

		public override List<CardAction> GetActions(State s, Combat c)
			=> upgrade switch
			{
				Upgrade.B => [
					new ASpawn { thing = new Asteroid() },
					new ASpawn { thing = new Asteroid(), offset = -1 },
					new ASpawn { thing = new AttackDrone() },
					new ASpawn { thing = new AttackDrone(), offset = -1 },
				],
				Upgrade.A => [
					new ASpawn { thing = new Asteroid() },
					new ASpawn { thing = new Missile { missileType = MissileType.corrode } },
				],
				_ => [
					new ASpawn { thing = new Asteroid() },
					new ASpawn { thing = new AttackDrone() },
				]
			};
	}
}
