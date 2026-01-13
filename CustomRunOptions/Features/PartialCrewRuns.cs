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
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DailyDescriptor), nameof(DailyDescriptor.GetDailyModifierArtifactKeys)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DailyDescriptor_GetDailyModifierArtifactKeys_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Randomize)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Randomize_Postfix))
		);

		void BlacklistEventDuringUnmannedRun(string nodeKey)
		{
			if (!DB.story.all.TryGetValue(nodeKey, out var node))
				return;
			node.doesNotHaveArtifacts ??= [];
			node.doesNotHaveArtifacts.Add(new UnmannedRunArtifact().Key());
		}
	}

	internal static StarterDeck MakeDefaultPartialDuoDeck(State state, Deck deck)
	{
		var result = new StarterDeck();

		if (deck == Deck.colorless)
		{
			result.cards.AddRange(
				NewRunOptions.allChars
					.Select(deck =>
					{
						if (deck == Deck.colorless)
							return null;
						if (ModEntry.Instance.EssentialsApi is { } essentialsApi)
							return essentialsApi.IsBlacklistedExeStarter(deck) ? null : essentialsApi.GetExeCardTypeForDeck(deck);
						if (ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(deck) is not { } characterEntry)
							return null;
						return characterEntry.Configuration.ExeCardType;
					})
					.OfType<Type>()
					.Select(exeCardType => (Card)Activator.CreateInstance(exeCardType)!)
					.Shuffle(state.rngCardOfferings)
					.Take(3)
			);
			return result;
		}

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
						return meta.deck == deck && meta is { unreleased: false, dontOffer: false };
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
					.Where(
						card => ModEntry.Instance.MoreDifficultiesApi is { } moreDifficultiesApi
						        && card.GetType() != moreDifficultiesApi.BasicOffencesCardType
						        && card.GetType() != moreDifficultiesApi.BasicDefencesCardType
						        && card.GetType() != moreDifficultiesApi.BasicManeuversCardType
					)
					.DistinctBy(card => card.Key())
			);
		
		return result;
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

		if (ModEntry.Instance.MoreDifficultiesApi is { } moreDifficultiesApi)
		{
			var mdoTypes = new[]
			{
				moreDifficultiesApi.BasicOffencesCardType,
				moreDifficultiesApi.BasicDefencesCardType,
				moreDifficultiesApi.BasicManeuversCardType,
				moreDifficultiesApi.BasicBroadcastCardType,
			};

			foreach (var mdoType in mdoTypes)
			{
				if (ModEntry.Instance.Helper.Content.Cards.LookupByCardType(mdoType) is not { } cardEntry)
					continue;
				dontRemoveThese.Add(cardEntry.UniqueName);
			}
		}
		
		if (StarterShip.ships.TryGetValue(state.ship.key, out var starterShip))
			foreach (var card in starterShip.cards)
				dontRemoveThese.Add(card.Key());
		
		state.deck.RemoveAll(card => !dontRemoveThese.Contains(card.Key()));
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

	private static void CardReward_GetOffering_Prefix(State s, Deck? limitDeck, Rarity? rarityOverride, out List<(Card Card, bool DontOffer, Deck Deck)>? __state)
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

		if (ModEntry.Instance.MoreDifficultiesApi is { } moreDifficultiesApi && s.GetDifficulty() >= moreDifficultiesApi.Difficulty1)
		{
			starterCardKeys.Remove(nameof(CannonColorless));
			starterCardKeys.Remove(nameof(BasicShieldColorless));
			starterCardKeys.Remove(nameof(DodgeColorless));
			
			var mdoTypes = new[]
			{
				moreDifficultiesApi.BasicOffencesCardType,
				moreDifficultiesApi.BasicDefencesCardType,
				moreDifficultiesApi.BasicManeuversCardType,
			};

			foreach (var mdoType in mdoTypes)
			{
				if (ModEntry.Instance.Helper.Content.Cards.LookupByCardType(mdoType) is not { } cardEntry)
					continue;
				starterCardKeys.Add(cardEntry.UniqueName);
			}
		}

		if (limitDeck == Deck.colorless)
		{
			// do nothing, normal behavior
		}
		else if (rarityOverride is not null)
		{
			var allowedRarityCardKey = rarityOverride switch
			{
				Rarity.common => nameof(DefensiveMode),
				Rarity.uncommon => nameof(JackOfAllTrades),
				Rarity.rare => nameof(AdaptabilityCard),
				_ => null,
			};

			foreach (var card in DB.releasedCards)
			{
				var meta = card.GetMeta();

				if (meta.deck != Deck.colorless)
					continue;

				var shouldOffer = card.Key() == allowedRarityCardKey;
				if (shouldOffer != meta.dontOffer)
					continue;

				__state.Add((card, meta.dontOffer, meta.deck));
				meta.dontOffer = !shouldOffer;
			}
		}
		else
		{
			foreach (var card in DB.releasedCards)
			{
				var meta = card.GetMeta();

				if (meta.deck != Deck.colorless)
					continue;

				var shouldOffer = starterCardKeys.Contains(card.Key());
				if (shouldOffer != meta.dontOffer)
					continue;

				__state.Add((card, meta.dontOffer, meta.deck));
				meta.deck = Deck.colorless;
				meta.dontOffer = !shouldOffer;
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

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> DailyDescriptor_GetDailyModifierArtifactKeys_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Call("Next"),
					ILMatches.LdcR8(0.6).Anchor(out var soloChanceInstruction),
					ILMatches.Instruction(OpCodes.Clt),
				])
				.Anchors().AnchorBlock(out var findBlock)
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Ldloc<IEnumerable<string>>(originalMethod).GetLocalIndex(out var possibleDailyArtifactKeysLocalIndex))
				.Anchors().PointerMatcher(soloChanceInstruction)
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DailyDescriptor_GetDailyModifierArtifactKeys_Transpiler_SoloChance))))
				.Anchors().BlockMatcher(findBlock)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloca, possibleDailyArtifactKeysLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DailyDescriptor_GetDailyModifierArtifactKeys_Transpiler_ModifyPossibleDailyArtifactKeys))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static double DailyDescriptor_GetDailyModifierArtifactKeys_Transpiler_SoloChance()
		=> ModEntry.Instance.Settings.ProfileBased.Current.SoloDailyChance;

	private static void DailyDescriptor_GetDailyModifierArtifactKeys_Transpiler_ModifyPossibleDailyArtifactKeys(ref IEnumerable<string> possibleDailyArtifactKeys, Rand random)
	{
		{
			var chance = ModEntry.Instance.Settings.ProfileBased.Current.DuoDailyChance;
			if (chance > 0 && chance >= random.Next())
				possibleDailyArtifactKeys = possibleDailyArtifactKeys.Append(new DuoRunArtifact().Key());
		}
		
		{
			var chance = ModEntry.Instance.Settings.ProfileBased.Current.UnmannedDailyChance;
			if (chance > 0 && chance >= random.Next())
				possibleDailyArtifactKeys = possibleDailyArtifactKeys.Append(new UnmannedRunArtifact().Key());
		}
	}

	private static void NewRunOptions_Randomize_Postfix(State s, Rand rng)
	{
		var settings = ModEntry.Instance.Settings.ProfileBased.Current;
		if (settings is { UnmannedRandomizeChance: <= 0, SoloRandomizeChance: <= 0, DuoRandomizeChance: <= 0 })
			return;

		var lockedSelectedChars = s.runConfig.selectedChars
			.Where(deck => ModEntry.Instance.MoreDifficultiesApi?.IsLocked(s, deck) == true)
			.ToHashSet();

		double? roll = null;
		
		if (lockedSelectedChars.Count == 0 && settings.UnmannedRandomizeChance > 0)
		{
			roll ??= rng.Next();
			if (roll < settings.UnmannedRandomizeChance)
			{
				s.runConfig.selectedChars.Clear();
				return;
			}
			roll -= settings.UnmannedRandomizeChance;
		}
		
		if (lockedSelectedChars.Count <= 1 && settings.SoloRandomizeChance > 0)
		{
			roll ??= rng.Next();
			if (roll < settings.SoloRandomizeChance)
			{
				s.runConfig.selectedChars = s.runConfig.selectedChars
					.OrderBy(deck => NewRunOptions.allChars.IndexOf(deck))
					.Shuffle(rng)
					.Take(1 - lockedSelectedChars.Count)
					.Concat(lockedSelectedChars)
					.ToHashSet();
				return;
			}
			roll -= settings.SoloRandomizeChance;
		}
		
		if (lockedSelectedChars.Count <= 2 && settings.DuoRandomizeChance > 0)
		{
			roll ??= rng.Next();
			if (roll < settings.DuoRandomizeChance)
			{
				s.runConfig.selectedChars = s.runConfig.selectedChars
					.OrderBy(deck => NewRunOptions.allChars.IndexOf(deck))
					.Shuffle(rng)
					.Take(2 - lockedSelectedChars.Count)
					.Concat(lockedSelectedChars)
					.ToHashSet();
				// return;
			}
			// roll -= settings.DuoRandomizeChance;
		}
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
			if (state.characters.Count < 2)
				return;
			if (state.characters.Any(character => character.deckType is null))
				return;

			if (state.characters.Count > 2)
			{
				var shuffleRng = state.rngCurrentEvent.Offshoot();
				state.characters = state.characters.Shuffle(shuffleRng).Take(2).ToList();
			}
			
			RemoveNormalStarters(state);

			if (DuoDecks.TryGetValue(new(state.characters[0].deckType!.Value, state.characters[1].deckType!.Value), out var duoDeck))
			{
				foreach (var card in duoDeck.cards)
					state.SendCardToDeck(card.CopyWithNewId());
				foreach (var artifact in duoDeck.artifacts)
					state.SendArtifactToChar(Mutil.DeepCopy(artifact));
				return;
			}

			var partialDuoDeck1 = PartialDuoDecks.GetValueOrDefault(state.characters[0].deckType!.Value) ?? MakeDefaultPartialDuoDeck(state, state.characters[0].deckType!.Value);
			var partialDuoDeck2 = PartialDuoDecks.GetValueOrDefault(state.characters[1].deckType!.Value) ?? MakeDefaultPartialDuoDeck(state, state.characters[1].deckType!.Value);
			
			foreach (var card in partialDuoDeck1.cards)
				state.SendCardToDeck(card.CopyWithNewId());
			foreach (var card in partialDuoDeck2.cards)
				state.SendCardToDeck(card.CopyWithNewId());
			
			foreach (var artifact in partialDuoDeck1.artifacts)
				state.SendArtifactToChar(Mutil.DeepCopy(artifact));
			foreach (var artifact in partialDuoDeck2.artifacts)
				state.SendArtifactToChar(Mutil.DeepCopy(artifact));
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
			{
				state.characters.Clear();
				RemoveNormalStarters(state);
			}

			var unmannedDeck = UnmannedDecks.GetValueOrDefault(state.ship.key) ?? MakeDefaultUnmannedDeck(state.ship.key);
			foreach (var card in unmannedDeck.cards)
				state.SendCardToDeck(card.CopyWithNewId());
			foreach (var artifact in unmannedDeck.artifacts)
				state.SendArtifactToChar(Mutil.DeepCopy(artifact));
		}
	}
}
