using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Legacy;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

public sealed class ModEntry : CobaltCoreModding.Definitions.ModManifests.IModManifest, IPrelaunchManifest, IApiProviderManifest, ISpriteManifest, IDeckManifest, IArtifactManifest, ICardManifest, INickelManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => [new DependencyEntry<CobaltCoreModding.Definitions.ModManifests.IModManifest>("Shockah.Kokoro", ignoreIfMissing: false)];

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }
	internal IModHelper Helper { get; private set; } = null!;
	internal IPluginPackage<Nickel.IModManifest> Package { get; private set; } = null!;

	internal IKokoroApi.IV2 KokoroApi { get; private set; } = null!;
	internal readonly DuoArtifactDatabase Database = new();
	internal ExternalSprite[] DuoGlowSprites { get; } = new ExternalSprite[2];
	internal ExternalSprite[] TrioGlowSprites { get; } = new ExternalSprite[3];

	private IHarmony Harmony { get; set; } = null!;
	private readonly Dictionary<HashSet<string>, ExternalSprite> DuoArtifactSprites = new(HashSet<string>.CreateSetComparer());

	internal Settings Settings { get; private set; } = new();

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		KokoroApi = contact.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;
	}

	public void OnNickelLoad(IPluginPackage<Nickel.IModManifest> package, IModHelper helper)
	{
		this.Helper = helper;
		this.Package = package;
		this.Harmony = helper.Utilities.Harmony;
		Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));

		ArtifactPatches.Apply(Harmony);
		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactRewardPatches.Apply(Harmony);
		CharacterPatches.Apply(Harmony);
		StatePatches.Apply(Harmony);

		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.ApplyPatches(Harmony);

		helper.ModRegistry.AwaitApi<IModSettingsApi>(
			"Nickel.ModSettings",
			api => api.RegisterModSettings(api.MakeList([
				api.MakeProfileSelector(
					() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
					Settings.ProfileBased
				),
				
				api.MakeEnumStepper(
					() => I18n.OfferingModeSettingName,
					() => Settings.ProfileBased.Current.OfferingMode,
					value => Settings.ProfileBased.Current.OfferingMode = value
				).SetValueFormatter(
					value => value switch
					{
						ProfileSettings.OfferingModeEnum.Common => I18n.OfferingModeSettingCommonValueName,
						ProfileSettings.OfferingModeEnum.Extra => I18n.OfferingModeSettingExtraValueName,
						ProfileSettings.OfferingModeEnum.ExtraOnceThenCommon => I18n.OfferingModeSettingExtraOnceThenCommonValueName,
						_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
					}
				).SetValueWidth(
					_ => 120
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.OfferingMode)}")
					{
						TitleColor = Colors.textBold,
						Title = I18n.OfferingModeSettingName,
						Description = I18n.OfferingModeSettingDescription
					}
				]),
				
				api.MakeCheckbox(
					() => I18n.ArtifactsConditionSettingName,
					() => Settings.ProfileBased.Current.ArtifactsCondition,
					(_, _, value) => Settings.ProfileBased.Current.ArtifactsCondition = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.ArtifactsCondition)}")
					{
						TitleColor = Colors.textBold,
						Title = I18n.ArtifactsConditionSettingName,
						Description = I18n.ArtifactsConditionSettingDescription
					}
				]),
				api.MakeConditional(
					api.MakeNumericStepper(
						() => I18n.MinArtifactsSettingName,
						() => Settings.ProfileBased.Current.MinArtifacts,
						value => Settings.ProfileBased.Current.MinArtifacts = value,
						minValue: 1
					),
					() => Settings.ProfileBased.Current.ArtifactsCondition
				),

				api.MakeCheckbox(
					() => I18n.RareCardsConditionSettingName,
					() => Settings.ProfileBased.Current.RareCardsCondition,
					(_, _, value) => Settings.ProfileBased.Current.RareCardsCondition = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.RareCardsCondition)}")
					{
						TitleColor = Colors.textBold,
						Title = I18n.RareCardsConditionSettingName,
						Description = I18n.RareCardsConditionSettingDescription
					}
				]),
				api.MakeConditional(
					api.MakeNumericStepper(
						() => I18n.MinRareCardsSettingName,
						() => Settings.ProfileBased.Current.MinRareCards,
						value => Settings.ProfileBased.Current.MinRareCards = value,
						minValue: 1
					),
					() => Settings.ProfileBased.Current.RareCardsCondition
				),

				api.MakeCheckbox(
					() => I18n.AnyCardsConditionSettingName,
					() => Settings.ProfileBased.Current.AnyCardsCondition,
					(_, _, value) => Settings.ProfileBased.Current.AnyCardsCondition = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.AnyCardsCondition)}")
					{
						TitleColor = Colors.textBold,
						Title = I18n.AnyCardsConditionSettingName,
						Description = I18n.AnyCardsConditionSettingDescription
					}
				]),
				api.MakeConditional(
					api.MakeNumericStepper(
						() => I18n.MinCardsSettingName,
						() => Settings.ProfileBased.Current.MinCards,
						value => Settings.ProfileBased.Current.MinCards = value,
						minValue: 1
					),
					() => Settings.ProfileBased.Current.AnyCardsCondition
				)
			]).SubscribeToOnMenuClose(_ =>
			{
				helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), Settings);
			}))
		);
	}

	public void FinalizePreperations(IPrelaunchContactPoint contact)
	{
		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.ApplyLatePatches(Harmony);
	}

	object IApiProviderManifest.GetApi(IManifest requestingMod)
		=> new ApiImplementation(Database);

	public void LoadManifest(ISpriteRegistry registry)
	{
		for (var i = 0; i < DuoGlowSprites.Length; i++)
			DuoGlowSprites[i] = registry.RegisterArtOrThrow(
				id: $"{typeof(ModEntry).Namespace}.DuoGlow.{i}",
				file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Effects", $"DuoGlow{i}.png"))
			);
		for (var i = 0; i < TrioGlowSprites.Length; i++)
			TrioGlowSprites[i] = registry.RegisterArtOrThrow(
				id: $"{typeof(ModEntry).Namespace}.TrioGlow.{i}",
				file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Effects", $"TrioGlow{i}.png"))
			);

		var namePrefix = $"{typeof(ModEntry).Namespace}.Sprite";
		foreach (var definition in DuoArtifactDefinition.Definitions)
		{
			DuoArtifactSprites[definition.CharacterKeys.Value] = registry.RegisterArtOrThrow(
				id: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}",
				file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Artifacts", $"{definition.AssetName}.png"))
			);
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.RegisterArt(registry, namePrefix, definition);
		}
	}

	public void LoadManifest(IDeckRegistry registry)
	{
		Database.DuoArtifactDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Duo",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.Black,
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_ephemeral),
			bordersOverSprite: null
		);
		registry.RegisterDeck(Database.DuoArtifactDeck);

		Database.TrioArtifactDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Trio",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.Black,
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_ephemeral),
			bordersOverSprite: null
		);
		registry.RegisterDeck(Database.TrioArtifactDeck);

		Database.ComboArtifactDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Combo",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.Black,
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_ephemeral),
			bordersOverSprite: null
		);
		registry.RegisterDeck(Database.ComboArtifactDeck);
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		foreach (var definition in DuoArtifactDefinition.Definitions)
		{
			var deck = definition.Characters.Count switch
			{
				2 => Database.DuoArtifactDeck,
				3 => Database.TrioArtifactDeck,
				_ => Database.ComboArtifactDeck
			};
			ExternalArtifact artifact = new(
				globalName: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}",
				artifactType: definition.Type,
				sprite: DuoArtifactSprites.GetValueOrDefault(definition.CharacterKeys.Value)!,
				ownerDeck: deck
			);
			artifact.AddLocalisation(definition.Name.ToUpper(), definition.Tooltip);
			registry.RegisterArtifact(artifact);
			Database.RegisterDuoArtifact(definition.Type, definition.Characters);
		}
	}

	public void LoadManifest(ICardRegistry registry)
	{
		string namePrefix = $"{typeof(ModEntry).Namespace}.Card";
		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.RegisterCards(registry, namePrefix, definition);
	}

	internal DuoArtifactEligibity GetDuoArtifactEligibity(Deck deck, State state)
	{
		if (deck == Deck.catartifact)
			deck = Deck.colorless;

		var decks = state.IsOutsideRun()
			? state.runConfig.selectedChars.ToHashSet()
			: state.characters.Select(c => c.deckType).WhereNotNull().ToHashSet();
		decks.Add(deck);

		DuoArtifactEligibity CheckDetailedEligibity()
		{
			var artifactsForThisCharacter = Database.GetAllDuoArtifactTypes()
				.Select(t => (Type: t, Ownership: Database.GetDuoArtifactTypeOwnership(t)!))
				.Where(e => e.Ownership.Any(owner => ArtifactDeckMatches(deck, owner)));

			if (!artifactsForThisCharacter.Any())
				return DuoArtifactEligibity.NoDuosForThisCharacter;

			var artifactsForThisCharacterInThisCrew = Database.GetMatchingDuoArtifactTypes(decks)
				.Select(t => (Type: t, Ownership: Database.GetDuoArtifactTypeOwnership(t)!))
				.Where(e => e.Ownership.Any(owner => ArtifactDeckMatches(deck, owner)));

			if (!artifactsForThisCharacterInThisCrew.Any())
				return DuoArtifactEligibity.NoDuosForThisCrew;

			return DuoArtifactEligibity.Eligible;
		}

		if (state.IsOutsideRun())
			return CheckDetailedEligibity();

		var character = state.characters.FirstOrDefault(c => deck == c.deckType);
		if (character is null)
			return DuoArtifactEligibity.InvalidState;

		var eligibleArtifacts = character.artifacts
			.Where(a => a.GetMeta().pools.Contains(ArtifactPool.Boss) || !a.GetMeta().unremovable)
			.ToList();

		if (Settings.ProfileBased.Current.ArtifactsCondition && eligibleArtifacts.Count >= Settings.ProfileBased.Current.MinArtifacts)
			return CheckDetailedEligibity();

		if (Settings.ProfileBased.Current.RareCardsCondition || Settings.ProfileBased.Current.AnyCardsCondition)
		{
			var characterCardsInDeck = state.GetAllCards()
				.Where(c => DB.cardMetas.TryGetValue(c.Key(), out var meta) && !meta.dontOffer && meta.deck == character.deckType)
				.Where(c => !c.GetDataWithOverrides(state).temporary)
				.ToList();

			if (Settings.ProfileBased.Current.AnyCardsCondition && characterCardsInDeck.Count >= Settings.ProfileBased.Current.MinCards)
				return CheckDetailedEligibity();

			var rareCharacterCardsInDeck = characterCardsInDeck
				.Where(c => DB.cardMetas.TryGetValue(c.Key(), out var meta) && (int)meta.rarity >= (int)Rarity.rare)
				.ToList();
			if (Settings.ProfileBased.Current.RareCardsCondition && rareCharacterCardsInDeck.Count >= Settings.ProfileBased.Current.MinRareCards)
				return CheckDetailedEligibity();
		}

		if (Settings.ProfileBased.Current is { ArtifactsCondition: false, RareCardsCondition: false, AnyCardsCondition: false })
			return CheckDetailedEligibity();

		return DuoArtifactEligibity.RequirementsNotSatisfied;
	}

	private static bool ArtifactDeckMatches(Deck characterDeck, Deck? otherDeck)
		=> Equals(characterDeck, otherDeck == Deck.catartifact ? Deck.colorless : otherDeck);
}
