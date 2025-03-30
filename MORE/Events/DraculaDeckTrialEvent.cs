using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class DraculaDeckTrialEvent : IRegisterable
{
	private static string EventName = null!;

	private static readonly List<IDeckTrial> AllTrials = [
		new TraitTrial { TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait.UniqueName, Count = 2 },
		new TraitTrial { TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.RetainCardTrait.UniqueName, Count = 2 },
		new TraitTrial { TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait.UniqueName, Count = 1 },
		new TraitTrial { TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.InfiniteCardTrait.UniqueName, Count = 1 },
		new TraitTrial { TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait.UniqueName, Count = 2 },
		new TraitTrial { TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.SingleUseCardTrait.UniqueName, Count = 1 },
		new CheapTrial(),
		new ExpensiveTrial(),
		new ExoticTrial(),
		new DuplicatesTrial(),
		new UpgradesTrial(),
		new ReaderTrial(),
		new PauperTrial(),
		new ConnoisseurTrial(),
		new CollectorTrial(),
		new FriendshipTrial(),
	];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		EventName = $"{package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}";

		var draculaEventNode = DB.story.all["DraculaTime"];

		DB.story.all[EventName] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = true,
			oncePerRun = true,
			zones = ["zone_lawless", "zone_three"],
			lines = [
				new InstructionWrapper { Instruction = draculaEventNode.lines[0], ScriptOverride = "DraculaTime" },
				new InstructionWrapper { Instruction = draculaEventNode.lines[1], ScriptOverride = "DraculaTime" },
				new InstructionWrapper { Instruction = draculaEventNode.lines[2], ScriptOverride = "DraculaTime" },
				new InstructionWrapper { Instruction = draculaEventNode.lines[3], ScriptOverride = "DraculaTime" },
				new CustomSay
				{
					who = "dracula",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "5-Dracula"])
				}
			],
			choiceFunc = EventName
		};
		DB.story.all[$"{EventName}::Success"] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = false,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "dracula",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-1-Dracula"])
				}
			],
			choiceFunc = $"{EventName}::Success"
		};
		DB.story.all[$"{EventName}::Failure"] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = false,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "dracula",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Failure-1-Dracula"])
				}
			],
			choiceFunc = $"{EventName}::Failure"
		};

		DB.eventChoiceFns[EventName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
		DB.eventChoiceFns[$"{EventName}::Success"] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetSuccessChoices));
		DB.eventChoiceFns[$"{EventName}::Failure"] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetFailureChoices));

		helper.Content.Enemies.RegisterEnemy(new()
		{
			EnemyType = typeof(TrialEnemy),
			ShouldAppearOnMap = (_, _) => null,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "DraculaDeckTrial", "TrialEnemy"]).Localize
		});

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			AllTrials.AddRange(NewRunOptions.allChars.Select(d => new CrewTrial { Deck = d }));
		};

		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnQueueEmptyDuringPlayerTurn), (Combat combat) =>
		{
			if (combat.otherShip.ai is not TrialEnemy trialEnemy || trialEnemy.StartedTrial)
				return;
			if (combat.currentCardAction is not null || combat.cardActions.Count != 0)
				return;

			trialEnemy.StartedTrial = true;
			combat.Queue([
				new ADelay { timer = 2 },
				new TrialResultsAction(),
			]);
		}, double.MinValue);
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		var node = DB.story.all[EventName];
		node.never = settings.DisabledEvents.Contains(MoreEvent.DraculaDeckTrial) ? true : null;
		node.dontCountForProgression = settings.DisabledEvents.Contains(MoreEvent.DraculaDeckTrial);
	}

	private static List<Choice> GetChoices(State state)
		=> AllTrials
			.Where(t => t.IsTrialApplicable(state))
			.Shuffle(state.rngCurrentEvent)
			.GroupBy(t => t.GetType())
			.Select(g => g.First())
			.Select(t =>
			{
				var choice = t.MakeChoice(state);
				if (choice is null)
					return null;

				choice.actions = [
					new AStartCombat { ai = new TrialEnemy { Trial = t } },
					new ATooltipAction { Tooltips = [
						new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::Setup")
						{
							TitleColor = Colors.textChoice,
							Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Setup", "name"]),
							Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Setup", "description"]),
						},
					] },
					.. choice.actions,
				];
				return choice;
			})
			.WhereNotNull()
			.Take(state.GetHardEvents() ? 3 : 4)
			.ToList();

	private static List<Choice> GetSuccessChoices(State state)
	{
		List<Choice> choices = [];

		if (state.deck.Select(c => c.GetDataWithOverrides(state)).Any(d => d is { cost: > 0, recycle: false }))
			choices.Add(new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddRecycle", "choice"]),
				actions = [
					new ACardSelect
					{
						browseAction = new CardSelectOverrideTraitForever
						{
							TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait.UniqueName,
							OverrideValue = true,
							Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddRecycle", "title"]),
							DoneTitle = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddRecycle", "done"]),
						},
						browseSource = CardBrowse.Source.Deck,
						filterMinCost = 1,
						filterTemporary = false
					}.SetFilterRecycle(false),
					new ATooltipAction { Tooltips = [new TTGlossary("cardtrait.recycle")] },
				]
			});

		if (state.deck.Any(c => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, c, ModEntry.Instance.Helper.Content.Cards.RetainCardTrait)))
			choices.Add(new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddRetain", "choice"]),
				actions = [
					new ACardSelect
					{
						browseAction = new CardSelectOverrideTraitForever
						{
							TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.RetainCardTrait.UniqueName,
							OverrideValue = true,
							Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddRetain", "title"]),
							DoneTitle = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddRetain", "done"]),
						},
						browseSource = CardBrowse.Source.Deck,
						filterRetain = false,
						filterTemporary = false
					},
					new ATooltipAction { Tooltips = [new TTGlossary("cardtrait.retain")] },
				]
			});

		if (state.deck.Any(c => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, c, ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait)))
			choices.Add(new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddBuoyant", "choice"]),
				actions = [
					new ACardSelect
					{
						browseAction = new CardSelectOverrideTraitForever
						{
							TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait.UniqueName,
							OverrideValue = true,
							Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddBuoyant", "title"]),
							DoneTitle = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "AddBuoyant", "done"]),
						},
						browseSource = CardBrowse.Source.Deck,
						filterBuoyant = false,
						filterTemporary = false
					},
					new ATooltipAction { Tooltips = [new TTGlossary("cardtrait.buoyant")] },
				]
			});

		if (state.deck.Any(c => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, c, ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait)))
			choices.Add(new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "RemoveExhaust", "choice"]),
				actions = [
					new ACardSelect
					{
						browseAction = new CardSelectOverrideTraitForever
						{
							TraitUniqueName = ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait.UniqueName,
							OverrideValue = false,
							Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "RemoveExhaust", "title"]),
							DoneTitle = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "RemoveExhaust", "done"]),
						},
						browseSource = CardBrowse.Source.Deck,
						filterExhaust = true,
						filterTemporary = false
					},
					new ATooltipAction { Tooltips = [new TTGlossary("cardtrait.exhaust")] },
				]
			});

		choices.Add(new()
		{
			label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Success-Choice", "None"])
		});

		return choices;
	}

	[UsedImplicitly]
	private static List<Choice> GetFailureChoices(State state)
		=> [
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Failure-Choice-OK"]),
				actions = [
					new ACardOffering
					{
						amount = 3,
						battleType = BattleType.Normal
					}
				]
			}
		];

	private sealed class InstructionWrapper : Instruction
	{
		public required Instruction Instruction;
		public string? ScriptOverride;

		public override bool Execute(G g, IScriptTarget target, ScriptCtx ctx)
		{
			var wrappedContext = Mutil.DeepCopy(ctx);
			if (ScriptOverride is not null)
				wrappedContext.script = ScriptOverride;
			return Instruction.Execute(g, target, wrappedContext);
		}
	}

	private sealed class CardSelectOverrideTraitForever : CardAction
	{
		public required string TraitUniqueName;
		public required bool? OverrideValue;
		public required string Title;
		public required string DoneTitle;

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			if (selectedCard is null)
				return null;
			if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(TraitUniqueName) is not { } trait)
				return null;

			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, trait, OverrideValue, permanent: true);
			return new CustomShowCards
			{
				messageKey = $"{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}::ShowCards::{TraitUniqueName}",
				Message = DoneTitle,
				cardIds = [selectedCard.uuid]
			};
		}

		public override string GetCardSelectText(State s)
			=> Title;
	}

	private sealed class CustomShowCards : ShowCards
	{
		public required string Message;

		public override void Render(G g)
		{
			DB.currentLocale.strings[messageKey] = Message;
			base.Render(g);
		}
	}

	public sealed class TrialEnemy : AI
	{
		public required IDeckTrial Trial;
		public bool StartedTrial;

		public override Ship BuildShipForSelf(State s)
		{
			character = new Character
			{
				type = "dracula"
			};

			Ship ship;
			if (StarterShip.ships.TryGetValue("Shockah.Dracula::Batmobile", out var batmobile))
				ship = Mutil.DeepCopy(batmobile.ship);
			else
				ship = new()
				{
					hull = 12,
					hullMax = 12,
					shieldMaxBase = 8,
					chassisUnder = "chassis_ancient",
					parts = [
						new()
						{
							type = PType.wing,
							skin = "wing_ancient",
						},
						new()
						{
							key = "bay.left",
							type = PType.missiles,
							skin = "missiles_ancient",
						},
						new()
						{
							type = PType.empty,
							skin = "scaffolding_ancient",
						},
						new()
						{
							key = "cockpit",
							type = PType.cockpit,
							skin = "cockpit_ancient",
						},
						new()
						{
							type = PType.empty,
							skin = "scaffolding_ancient",
						},
						new()
						{
							key = "bay.right",
							type = PType.missiles,
							skin = "missiles_ancient",
						},
						new()
						{
							type = PType.wing,
							skin = "wing_ancient",
							flip = true,
						},
					]
				};

			ship.isPlayerShip = false;
			ship.x = 6;
			ship.ai = this;
			ship.Set(Status.perfectShield, 2);
			return ship;
		}
	}

	public sealed class TrialResultsAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			if (c.otherShip.ai is not TrialEnemy trialEnemy)
				return;
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (trialEnemy.Trial is null) // someone is screwing with the debug menu...
				return;

			var success = trialEnemy.Trial.TestCards(s, c.hand);
			c.QueueImmediate([
				new AMidCombatDialogue { script = success ? $"{EventName}::Success" : $"{EventName}::Failure" },
				new AEscape { targetPlayer = false }
			]);
		}
	}

	public interface IDeckTrial
	{
		bool IsTrialApplicable(State state) => TestCards(state, state.deck);
		Choice? MakeChoice(State state);
		bool TestCards(State state, IReadOnlyList<Card> cards);
	}

	public sealed class CrewTrial : IDeckTrial
	{
		public required Deck Deck;

		public bool IsTrialApplicable(State state)
			=> state.characters.Any(c => c.deckType == Deck) && TestCards(state, state.deck);

		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Crew", "name"], new { Deck = Loc.T($"char.{Deck.Key()}") }),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Crew", "name"], new { Deck = Loc.T($"char.{Deck.Key()}") }),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Crew", "description"], new { Deck = Loc.T($"char.{Deck.Key()}"), DeckColor = DB.decks[Deck].color.ToString() }),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> hand)
			=> hand.Count(c => c.GetMeta().deck == Deck) >= 2;
	}

	public sealed class TraitTrial : IDeckTrial
	{
		public required string TraitUniqueName;
		public required int Count;

		public Choice? MakeChoice(State state)
		{
			if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(TraitUniqueName) is not { } trait)
				return null;
			if (trait.Configuration.Name is not { } nameProvider)
				return null;

			try
			{
				var traitName = nameProvider(DB.currentLocale.locale);
				if (string.IsNullOrEmpty(traitName))
					return null;
				var traitCapitalizedName = CultureInfo.GetCultureInfo("en-GB").TextInfo.ToTitleCase(traitName);

				return this.Count switch
				{
					1 => new()
					{
						label = ModEntry.Instance.Localizations.Localize([
							"event", "DraculaDeckTrial", "Choice", "Trait", "name"
						], new
						{
							Trait = traitCapitalizedName
						}),
						actions =
						[
							new ATooltipAction
							{
								Tooltips =
								[
									new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{this.GetType().Name}")
									{
										TitleColor = Colors.textChoice,
										Title = ModEntry.Instance.Localizations.Localize([
											"event", "DraculaDeckTrial", "Choice", "Trait", "name"
										], new
										{
											Trait = traitCapitalizedName
										}),
										Description = ModEntry.Instance.Localizations.Localize([
											"event", "DraculaDeckTrial", "Choice", "Trait", "description", "one"
										], new
										{
											Trait = traitName
										}),
									},
									.. (trait.Configuration.Tooltips?.Invoke(state, null) ?? [])
								]
							}
						]
					},
					_ => new()
					{
						label = ModEntry.Instance.Localizations.Localize([
							"event", "DraculaDeckTrial", "Choice", "Trait", "name"
						], new
						{
							Trait = traitCapitalizedName
						}),
						actions =
						[
							new ATooltipAction
							{
								Tooltips =
								[
									new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{this.GetType().Name}")
									{
										TitleColor = Colors.textChoice,
										Title = ModEntry.Instance.Localizations.Localize([
											"event", "DraculaDeckTrial", "Choice", "Trait", "name"
										], new
										{
											Trait = traitCapitalizedName
										}),
										Description = ModEntry.Instance.Localizations.Localize([
											"event", "DraculaDeckTrial", "Choice", "Trait", "description", "other"
										], new
										{
											Trait = traitName,
											Count = this.Count
										}),
									},
									.. (trait.Configuration.Tooltips?.Invoke(state, null) ?? [])
								]
							}
						]
					}
				};
			}
			catch
			{
				return null;
			}
		}

		public bool TestCards(State state, IReadOnlyList<Card> cards)
		{
			if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(TraitUniqueName) is not { } trait)
				return false;
			return cards.Count(c => ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, c, trait)) >= Count;
		}
	}

	public sealed class CheapTrial : IDeckTrial
	{
		public bool IsTrialApplicable(State state)
			=> TestCards(state, state.deck.OrderBy(c => c.GetDataWithOverrides(state).cost).Take(state.ship.baseDraw).ToList());

		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Cheap", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Cheap", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Cheap", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Sum(c => Math.Max(c.GetDataWithOverrides(state).cost, 0)) <= 3;
	}

	public sealed class ExpensiveTrial : IDeckTrial
	{
		public bool IsTrialApplicable(State state)
			=> TestCards(state, state.deck.OrderByDescending(c => c.GetDataWithOverrides(state).cost).Take(state.ship.baseDraw).ToList());

		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Expensive", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Expensive", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Expensive", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Sum(c => Math.Max(c.GetDataWithOverrides(state).cost, 0)) >= 7;
	}

	public sealed class ExoticTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Exotic", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Exotic", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Exotic", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Count(c =>
			{
				var deck = c.GetMeta().deck;
				return deck != Deck.colorless && state.characters.All(character => character.deckType != deck);
			}) >= 2;
	}

	public sealed class DuplicatesTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Duplicates", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Duplicates", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Duplicates", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.GroupBy(c => c.Key()).Any(g => g.Count() >= 2);
	}

	public sealed class UpgradesTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Upgrades", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Upgrades", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Upgrades", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Count(c => c.upgrade != Upgrade.None) >= 3;
	}

	public sealed class ReaderTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Reader", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Reader", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Reader", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Count(c => !string.IsNullOrEmpty(c.GetDataWithOverrides(state).description)) >= 2;
	}

	public sealed class PauperTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Pauper", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Pauper", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Pauper", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.All(c => c.GetMeta().rarity == Rarity.common);
	}

	public sealed class ConnoisseurTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Connoisseur", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Connoisseur", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Connoisseur", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Count(c => c.GetMeta().rarity == Rarity.uncommon) >= 2;
	}

	public sealed class CollectorTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Collector", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Collector", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Collector", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> cards.Count(c => c.GetMeta().rarity == Rarity.rare) >= 2;
	}

	public sealed class FriendshipTrial : IDeckTrial
	{
		public Choice MakeChoice(State state)
			=> new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Friendship", "name"]),
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"event.{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					TitleColor = Colors.textChoice,
					Title = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Friendship", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "DraculaDeckTrial", "Choice", "Friendship", "description"]),
				}] }]
			};

		public bool TestCards(State state, IReadOnlyList<Card> cards)
			=> state.characters.Select(character => character.deckType).WhereNotNull().All(deck => cards.Any(card => card.GetMeta().deck == deck));
	}
}