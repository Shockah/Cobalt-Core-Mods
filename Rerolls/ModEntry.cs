using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Rerolls;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal Harmony Harmony { get; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		helper.Content.Artifacts.RegisterArtifact("Rerolls", new()
		{
			ArtifactType = typeof(RerollArtifact),
			Meta = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/RerollArtifact.png")).Sprite,
			Name = AnyLocalizations.Bind(["artifact", "name"]).Localize,
			Description = AnyLocalizations.Bind(["artifact", "description"]).Localize
		});

		_ = new ArtifactRerollManager();
		_ = new CardRerollManager();
		_ = new RunStartManager();
		_ = new RerollChargeManager();
	}
}
