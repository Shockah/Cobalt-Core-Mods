using System;
using System.Collections.Generic;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

public sealed class ApiImplementation(IModManifest mod) : ICustomRunOptionsApi
{
	public void RegisterBootSequenceUpside(string name, Func<string> title, Func<Choice, bool> matchPredicate)
	{
		var key = $"{mod.UniqueName}::{name}";
		BootSequenceCustomRunOption.UpsideChoices[key] = new BootSequenceCustomRunOption.IBootChoice.Modded(key, title, matchPredicate);
	}

	public void RegisterBootSequenceDownside(string name, Func<string> title, Func<Choice, bool> matchPredicate)
	{
		var key = $"{mod.UniqueName}::{name}";
		BootSequenceCustomRunOption.DownsideChoices[key] = new BootSequenceCustomRunOption.IBootChoice.Modded(key, title, matchPredicate);
	}

	public bool IsStartingNormalRun
		=> StartRunDetector.StartingNormalRun;

	public double SeedCustomRunOptionPriority
		=> SeedCustomRunOption.PRIORITY;
	
	public double BootSequenceCustomRunOptionPriority
		=> BootSequenceCustomRunOption.PRIORITY;
	
	public double DailyModifiersCustomRunOptionPriority
		=> DailyModifiersCustomRunOption.PRIORITY;

	public void RegisterCustomRunOption(ICustomRunOptionsApi.ICustomRunOption option, double priority = 0)
		=> ModEntry.Instance.CustomRunOptions.Add(option, priority);

	public void UnregisterCustomRunOption(ICustomRunOptionsApi.ICustomRunOption option)
		=> ModEntry.Instance.CustomRunOptions.Remove(option);

	public ICustomRunOptionsApi.INewRunOptionsElement.IIcon MakeIconNewRunOptionsElement(Spr icon, int? width = null, int? height = null)
		=> new IconNewRunOptionsElement(icon, width, height);

	public ICustomRunOptionsApi.INewRunOptionsElement.IArtifact MakeArtifactNewRunOptionsElement(Artifact artifact)
		=> new ArtifactNewRunOptionsElement(artifact);

	public ICustomRunOptionsApi.IIconAffixModSetting MakeIconAffixModSetting(IModSettingsApi.IModSetting setting)
		=> new IconAffixModSetting { Setting = setting };

	public ICustomRunOptionsApi.IIconAffixModSetting.IIconSetting MakeIconAffixModSettingIconSetting()
		=> new IconAffixModSetting.IconConfiguration();

	public void RegisterDuoDeck(Deck deck1, Deck deck2, StarterDeck starterDeck)
		=> PartialCrewRuns.DuoDecks[new(deck1, deck2)] = starterDeck;

	public void RegisterPartialDuoDeck(Deck deck, StarterDeck starterDeck)
		=> PartialCrewRuns.PartialDuoDecks[deck] = starterDeck;

	public void RegisterUnmannedDeck(string shipKey, StarterDeck starterDeck)
		=> PartialCrewRuns.UnmannedDecks[shipKey] = starterDeck;

	public StarterDeck? GetDuoDeck(Deck deck1, Deck deck2)
		=> PartialCrewRuns.DuoDecks.GetValueOrDefault(new(deck1, deck2));

	public StarterDeck? GetPartialDuoDeck(Deck deck)
		=> PartialCrewRuns.PartialDuoDecks.GetValueOrDefault(deck);

	public StarterDeck? GetUnmannedDuoDeck(string shipKey)
		=> PartialCrewRuns.UnmannedDecks.GetValueOrDefault(shipKey);

	public StarterDeck MakeDefaultPartialDuoDeck(Deck deck)
		=> PartialCrewRuns.MakeDefaultPartialDuoDeck(deck);

	public StarterDeck MakeDefaultUnmannedDeck(string shipKey)
		=> PartialCrewRuns.MakeDefaultUnmannedDeck(shipKey);
}