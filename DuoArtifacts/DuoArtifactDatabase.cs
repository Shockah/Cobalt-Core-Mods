﻿using CobaltCoreModding.Definitions.ExternalItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Shockah.DuoArtifacts;

internal sealed class DuoArtifactDatabase
{
	private const double SingleColorTransitionAnimationLengthSeconds = 2;
	private static readonly string FirstZoneDuoTag = $"{typeof(ModEntry).Namespace!}.Duo.FirstZone";
	private static readonly string PastFirstZoneDuoTag = $"{typeof(ModEntry).Namespace!}.Duo.PastFirstZone";

	internal ExternalDeck DuoArtifactDeck { get; set; } = null!;
	internal ExternalDeck TrioArtifactDeck { get; set; } = null!;
	internal ExternalDeck ComboArtifactDeck { get; set; } = null!;
	private readonly Dictionary<HashSet<Deck>, HashSet<Type>> ComboToTypeDictionary = new(HashSet<Deck>.CreateSetComparer());
	private readonly Dictionary<Type, HashSet<Deck>> TypeToComboDictionary = [];

	public bool IsDuoArtifactType(Type type)
		=> TypeToComboDictionary.ContainsKey(type);

	public bool IsDuoArtifact(Artifact artifact)
		=> IsDuoArtifactType(artifact.GetType());

	public IReadOnlySet<Deck>? GetDuoArtifactTypeOwnership(Type type)
		=> TypeToComboDictionary.GetValueOrDefault(type);

	public IReadOnlySet<Deck>? GetDuoArtifactOwnership(Artifact artifact)
		=> GetDuoArtifactTypeOwnership(artifact.GetType());

	public IEnumerable<Type> GetAllDuoArtifactTypes()
		=> TypeToComboDictionary.Keys;

	public IEnumerable<Artifact> InstantiateAllDuoArtifacts()
		=> TypeToComboDictionary.Keys.Select(t => (Artifact)Activator.CreateInstance(t)!);

	public IEnumerable<Type> GetExactDuoArtifactTypes(IEnumerable<Deck> combo)
		=> ComboToTypeDictionary.TryGetValue(FixCombo(combo.ToHashSet()), out var types) ? types : Array.Empty<Type>();

	public IEnumerable<Artifact> InstantiateExactDuoArtifacts(IEnumerable<Deck> combo)
		=> GetExactDuoArtifactTypes(combo).Select(t => (Artifact)Activator.CreateInstance(t)!);

	public IEnumerable<Type> GetMatchingDuoArtifactTypes(IEnumerable<Deck> combo)
	{
		var comboSet = new HashSet<Deck>(combo);
		foreach (var (keySet, types) in ComboToTypeDictionary)
			if (!keySet.Except(comboSet).Any())
				foreach (var type in types)
					yield return type;
	}

	public IEnumerable<Artifact> InstantiateMatchingDuoArtifacts(IEnumerable<Deck> combo)
		=> GetMatchingDuoArtifactTypes(combo).Select(t => (Artifact)Activator.CreateInstance(t)!);

	public Color GetDynamicColorForArtifact(Artifact artifact, Deck? ignoreDeck = null)
	{
		var colors = GetDuoArtifactOwnership(artifact)
			?.Select(c => c == Deck.catartifact ? Deck.colorless : c)
			.OrderBy(NewRunOptions.allChars.IndexOf)
			.Where(key => key != ignoreDeck)
			.Select(key => DB.decks[key].color)
			.ToList();
		if (colors is null || colors.Count == 0)
			return DB.decks[artifact.GetMeta().owner].color;
		if (colors.Count == 1)
			return colors[0];

		var animationLength = colors.Count * SingleColorTransitionAnimationLengthSeconds;
		var animationPosition = MG.inst.g.time % animationLength;
		var totalFraction = animationPosition / animationLength;
		var (fromColor, toColor, fraction) = GetLerpInfo(colors, totalFraction);
		var lerpFraction = Math.Sin(fraction * Math.PI) * 0.5 + 0.5;
		return Color.Lerp(fromColor, toColor, lerpFraction);

		static (Color, Color, double) GetLerpInfo(List<Color> colors, double totalFraction)
		{
			var singleFraction = 1.0 / colors.Count;
			var whichFraction = ((int)Math.Round(totalFraction / singleFraction) + colors.Count - 1) % colors.Count;
			var fractionStart = singleFraction * whichFraction;
			var fractionEnd = singleFraction * (whichFraction + 1);
			var fraction = (totalFraction - fractionStart) / (fractionEnd - fractionStart);
			return (colors[whichFraction], colors[(whichFraction + 1) % colors.Count], fraction);
		}
	}

	public void RegisterDuoArtifact(Type type, IEnumerable<Deck> combo)
	{
		if (TypeToComboDictionary.ContainsKey(type))
			throw new ArgumentException($"Artifact type {type} is already registered as a duo.");
		if (!type.IsAssignableTo(typeof(Artifact)))
			throw new ArgumentException($"Type {type} is not a subclass of the {typeof(Artifact)} type.");

		var comboSet = FixCombo(combo.ToHashSet());
		if (comboSet.Count < 2)
			throw new ArgumentException("Tried to register a duo artifact for less than 2 characters.");
		TypeToComboDictionary[type] = comboSet;

		ref var types = ref CollectionsMarshal.GetValueRefOrAddDefault(ComboToTypeDictionary, comboSet, out var typesExists);
		if (!typesExists)
			types = [];
		types!.Add(type);
	}

	private static HashSet<Deck> FixCombo(HashSet<Deck> combo)
	{
		if (!combo.Contains(Deck.catartifact))
			return combo;

		var result = new HashSet<Deck>(combo);
		result.Remove(Deck.catartifact);
		result.Add(Deck.colorless);
		return result;
	}

	internal static string GetStoryVarsTagForMap(MapBase map)
		=> map is MapFirst ? FirstZoneDuoTag : PastFirstZoneDuoTag;

	internal void FixArtifactMeta(ProfileSettings.OfferingModeEnum offeringMode, State? state, HashSet<string>? seenArtifacts)
	{
		if (state is null || state.storyVars.oncePerRunTags.Contains(GetStoryVarsTagForMap(state.map)))
			offeringMode = ProfileSettings.OfferingModeEnum.Extra;
		if (seenArtifacts is null && offeringMode == ProfileSettings.OfferingModeEnum.ExtraOnceThenCommon)
			offeringMode = ProfileSettings.OfferingModeEnum.Extra;

		foreach (var duoType in GetAllDuoArtifactTypes())
		{
			if (ModEntry.Instance.Helper.Content.Artifacts.LookupByArtifactType(duoType) is not { } duoEntry)
				continue;
			
			DB.artifactMetas[duoEntry.UniqueName].pools = offeringMode switch
			{
				ProfileSettings.OfferingModeEnum.Common => [ArtifactPool.Common],
				ProfileSettings.OfferingModeEnum.Extra => [ArtifactPool.EventOnly],
				ProfileSettings.OfferingModeEnum.ExtraOnceThenCommon => seenArtifacts!.Contains(duoEntry.UniqueName) ? [ArtifactPool.Common] : [ArtifactPool.EventOnly],
				_ => throw new ArgumentOutOfRangeException(nameof(offeringMode), offeringMode, null)
			};
			DB.artifactMetas[duoEntry.UniqueName].owner = offeringMode switch
			{
				ProfileSettings.OfferingModeEnum.Common => Deck.colorless,
				ProfileSettings.OfferingModeEnum.Extra => GetRealDuoDeck(),
				ProfileSettings.OfferingModeEnum.ExtraOnceThenCommon => seenArtifacts!.Contains(duoEntry.UniqueName) ? Deck.colorless : GetRealDuoDeck(),
				_ => throw new ArgumentOutOfRangeException(nameof(offeringMode), offeringMode, null)
			};

			Deck GetRealDuoDeck()
				=> (GetDuoArtifactTypeOwnership(duoType)?.Count ?? 0) switch
				{
					2 => (Deck)DuoArtifactDeck.Id!.Value,
					3 => (Deck)TrioArtifactDeck.Id!.Value,
					_ => (Deck)ComboArtifactDeck.Id!.Value,
				};
		}
	}
}