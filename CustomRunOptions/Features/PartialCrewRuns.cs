using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.CustomRunOptions;

internal sealed class PartialCrewRuns : IRegisterable
{
	internal static readonly Dictionary<UnorderedPair<Deck>, StarterDeck> DuoDecks = [];
	internal static readonly Dictionary<Deck, StarterDeck> PartialDuoDecks = [];
	internal static readonly Dictionary<string, StarterDeck> UnmannedDecks = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		DuoRunArtifact.Register(package, helper);
		UnmannedRunArtifact.Register(package, helper);

		BlacklistEventDuringUnmannedRun("ChoiceCardRewardOfYourColorChoice");
		BlacklistEventDuringUnmannedRun("CrystallizedFriendEvent");
		BlacklistEventDuringUnmannedRun("LoseCharacterCard");
		BlacklistEventDuringUnmannedRun("WrenTreat");
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.IsValid)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_IsValid_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: typeof(State).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PopulateRun>") && m.ReturnType == typeof(Route)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Delegate_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Finalizer))
		);

		void BlacklistEventDuringUnmannedRun(string nodeKey)
		{
			if (!DB.story.all.TryGetValue(nodeKey, out var node))
				return;
			node.doesNotHaveArtifacts ??= [];
			node.doesNotHaveArtifacts.Add(new UnmannedRunArtifact().Key());
		}
	}

	internal static StarterDeck MakeDefaultPartialDuoDeck(Deck deck)
	{
		var result = new StarterDeck();

		if (StarterDeck.starterSets.TryGetValue(deck, out var starterDeck))
		{
			result.cards.AddRange(starterDeck.cards);
			result.artifacts.AddRange(starterDeck.artifacts);
		}

		if (ModEntry.Instance.MoreDifficultiesApi is { } moreDifficultiesApi && moreDifficultiesApi.GetAltStarters(deck) is { } altDeck)
		{
			result.cards.AddRange(
				altDeck.cards
					.Where(card => result.cards.All(resultCard => resultCard.Key() != card.Key()))
					.Take(altDeck.cards.Count / 2)
			);
		}
		else if (SoloStarterDeck.soloStarterSets.TryGetValue(deck, out var soloDeck))
		{
			result.cards.AddRange(
				soloDeck.cards
					.Where(card => result.cards.All(resultCard => resultCard.Key() != card.Key()))
					.Take(Math.Max(result.cards.Count / 2, 1))
			);
		}
		else
		{
			result.cards.AddRange(
				DB.releasedCards
					.Where(card =>
					{
						var meta = card.GetMeta();
						return meta.deck == deck && !meta.unreleased && !meta.dontOffer;
					})
					.Where(card => result.cards.All(resultCard => resultCard.Key() != card.Key()))
					.Take(Math.Max(result.cards.Count / 2, 1))
			);
		}
		
		return result;
	}

	internal static StarterDeck MakeDefaultUnmannedDeck(string shipKey)
	{
		var result = new StarterDeck();

		result.cards.Add(new DefensiveMode());
		result.cards.Add(new JackOfAllTrades());
		result.cards.Add(new AdaptabilityCard());

		if (StarterShip.ships.TryGetValue(shipKey, out var starterShip))
			result.cards.AddRange(
				starterShip.cards
					.Where(card => card.Key() is not (nameof(CannonColorless) or nameof(BasicShieldColorless) or nameof(DodgeColorless)))
					.DistinctBy(card => card.Key())
			);
		
		return result;
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> RunConfig_IsValid_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("selectedChars"),
					ILMatches.Call("get_Count"),
					ILMatches.LdcI4(3),
					ILMatches.Beq.GetBranchTarget(out var continueBranchLabel),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_IsValid_Transpiler_ShouldAllowCharacterCountAnyway))),
					new CodeInstruction(OpCodes.Brtrue, continueBranchLabel.Value),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool RunConfig_IsValid_Transpiler_ShouldAllowCharacterCountAnyway(RunConfig config)
		=> config.selectedChars.Count < 3;

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> State_PopulateRun_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld<State>().Element(out var ldfldInstruction),
				])
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("giveRunStartRewards"),
					ILMatches.Brfalse,
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldfld, ldfldInstruction.Value.operand),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Delegate_Transpiler_ApplyPartialCrewRun))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void State_PopulateRun_Delegate_Transpiler_ApplyPartialCrewRun(State state)
	{
		switch (state.characters.Count)
		{
			case 0:
				state.SendArtifactToChar(new UnmannedRunArtifact());
				break;
			case 1:
				state.SendArtifactToChar(new DailyJustOneCharacter());
				break;
			case 2:
				state.SendArtifactToChar(new DuoRunArtifact());
				break;
		}
	}

	private static void CardReward_GetOffering_Prefix(State s, out List<(Card Card, bool DontOffer, Deck Deck)>? __state)
	{
		if (s.characters.Count != 0)
		{
			__state = null;
			return;
		}

		__state = [];
		s.characters.Add(new() { deckType = Deck.colorless });
		var starterCardKeys = StarterShip.ships.TryGetValue(s.ship.key, out var starterShip) ? starterShip.cards.Select(card => card.Key()).ToHashSet() : [
			nameof(CannonColorless),
			nameof(BasicShieldColorless),
			nameof(DodgeColorless),
		];

		foreach (var card in DB.releasedCards)
		{
			var meta = card.GetMeta();

			if (starterCardKeys.Contains(card.Key()))
			{
				if (meta.deck != Deck.colorless || meta.dontOffer)
				{
					__state.Add((card, meta.dontOffer, meta.deck));
					meta.deck = Deck.colorless;
					meta.dontOffer = false;
				}
			}
			else if (meta.deck == Deck.colorless)
			{
				if (!meta.dontOffer)
				{
					__state.Add((card, meta.dontOffer, meta.deck));
					meta.dontOffer = true;
				}
			}
		}
	}

	private static void CardReward_GetOffering_Finalizer(State s, in List<(Card Card, bool DontOffer, Deck Deck)>? __state)
	{
		if (__state is null)
			return;

		foreach (var entry in __state)
		{
			var meta = entry.Card.GetMeta();
			meta.deck = entry.Deck;
			meta.dontOffer = entry.DontOffer;
		}
		s.characters.RemoveAt(s.characters.Count - 1);
	}

	private sealed class DuoRunArtifact : Artifact, IRegisterable
	{
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
		{
			helper.Content.Artifacts.RegisterArtifact("PartialCrewDuoRun", new()
			{
				ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					owner = Deck.colorless,
					pools = [ArtifactPool.DailyStarterDeckMod, ArtifactPool.Unreleased],
					unremovable = true,
				},
				Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/PartialCrewDuoRun.png")).Sprite,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PartialCrewDuoRun", "name"]).Localize,
				Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PartialCrewDuoRun", "description"]).Localize
			});
		}

		public override void OnReceiveArtifact(State state)
		{
			if (state.characters.Count != 2)
				return;
			if (state.characters.Any(character => character.deckType is null))
				return;
			
			RemoveNormalStarters(state);

			if (DuoDecks.TryGetValue(new(state.characters[0].deckType!.Value, state.characters[1].deckType!.Value), out var duoDeck))
			{
				foreach (var card in duoDeck.cards)
					state.SendCardToDeck(card.CopyWithNewId());
				foreach (var artifact in duoDeck.artifacts)
					state.SendArtifactToChar(Mutil.DeepCopy(artifact));
				return;
			}

			var partialDuoDeck1 = PartialDuoDecks.GetValueOrDefault(state.characters[0].deckType!.Value) ?? MakeDefaultPartialDuoDeck(state.characters[0].deckType!.Value);
			var partialDuoDeck2 = PartialDuoDecks.GetValueOrDefault(state.characters[1].deckType!.Value) ?? MakeDefaultPartialDuoDeck(state.characters[1].deckType!.Value);
			
			foreach (var card in partialDuoDeck1.cards)
				state.SendCardToDeck(card.CopyWithNewId());
			foreach (var card in partialDuoDeck2.cards)
				state.SendCardToDeck(card.CopyWithNewId());
			
			foreach (var artifact in partialDuoDeck1.artifacts)
				state.SendArtifactToChar(Mutil.DeepCopy(artifact));
			foreach (var artifact in partialDuoDeck2.artifacts)
				state.SendArtifactToChar(Mutil.DeepCopy(artifact));
		}
		
		private static void RemoveNormalStarters(State state)
		{
			var dontRemoveThese = new HashSet<string>
			{
				nameof(CannonColorless),
				nameof(DodgeColorless),
				nameof(BasicShieldColorless),
				nameof(DroneshiftColorless),
				nameof(BasicSpacer),
				nameof(CorruptedCore),
			};
		
			if (StarterShip.ships.TryGetValue(state.ship.key, out var starterShip))
				foreach (var card in starterShip.cards)
					dontRemoveThese.Add(card.Key());
		
			state.deck.RemoveAll(card => !dontRemoveThese.Contains(card.Key()));
		}
	}

	private sealed class UnmannedRunArtifact : Artifact, IRegisterable
	{
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
		{
			helper.Content.Artifacts.RegisterArtifact("PartialCrewUnmannedRun", new()
			{
				ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					owner = Deck.colorless,
					pools = [ArtifactPool.DailyStarterDeckMod, ArtifactPool.Unreleased],
					unremovable = true,
				},
				Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/PartialCrewUnmannedRun.png")).Sprite,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PartialCrewUnmannedRun", "name"]).Localize,
				Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PartialCrewUnmannedRun", "description"]).Localize
			});
		}

		public override void OnReceiveArtifact(State state)
		{
			if (state.characters.Count != 0)
				return;

			var unmannedDeck = UnmannedDecks.GetValueOrDefault(state.ship.key) ?? MakeDefaultUnmannedDeck(state.ship.key);
			foreach (var card in unmannedDeck.cards)
				state.SendCardToDeck(card.CopyWithNewId());
			foreach (var artifact in unmannedDeck.artifacts)
				state.SendArtifactToChar(Mutil.DeepCopy(artifact));
		}
	}
}
