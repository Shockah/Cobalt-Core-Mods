using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.IO;

namespace Shockah.Kokoro;

internal sealed class Content
{
	private static ModEntry Instance => ModEntry.Instance;

	internal ExternalSprite WormSprite { get; private set; } = null!;
	internal ExternalStatus WormStatus { get; private set; } = null!;

	internal ExternalSprite OxidationSprite { get; private set; } = null!;
	internal ExternalStatus OxidationStatus { get; private set; } = null!;

	internal ExternalSprite QuestionMarkSprite { get; private set; } = null!;
	internal ExternalSprite EqualSprite { get; private set; } = null!;
	internal ExternalSprite NotEqualSprite { get; private set; } = null!;
	internal ExternalSprite GreaterThanSprite { get; private set; } = null!;
	internal ExternalSprite LessThanSprite { get; private set; } = null!;
	internal ExternalSprite GreaterThanOrEqualSprite { get; private set; } = null!;
	internal ExternalSprite LessThanOrEqualSprite { get; private set; } = null!;

	internal ExternalSprite EnergySprite { get; private set; } = null!;
	internal ExternalSprite ContinueSprite { get; private set; } = null!;
	internal ExternalSprite StopSprite { get; private set; } = null!;

	internal void RegisterArt(ISpriteRegistry registry)
	{
		WormSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.Worm",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "WormStatus.png"))
		);
		OxidationSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.Oxidation",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "OxidationStatus.png"))
		);
		QuestionMarkSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.QuestionMark",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "QuestionMark.png"))
		);
		EqualSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.Equal",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "Equal.png"))
		);
		NotEqualSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.NotEqual",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "NotEqual.png"))
		);
		GreaterThanSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.GreaterThan",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "GreaterThan.png"))
		);
		LessThanSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.LessThan",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "LessThan.png"))
		);
		GreaterThanOrEqualSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.GreaterThanOrEqual",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "GreaterThanOrEqual.png"))
		);
		LessThanOrEqualSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Conditional.LessThanOrEqual",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Conditional", "LessThanOrEqual.png"))
		);
		EnergySprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Energy",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Energy.png"))
		);
		ContinueSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Continue",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Continue.png"))
		);
		StopSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Stop",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Stop.png"))
		);
	}

	internal void RegisterStatuses(IStatusRegistry registry)
	{
		{
			WormStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Worm",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF009900)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF879900)),
				WormSprite,
				affectedByTimestop: false
			);
			WormStatus.AddLocalisation(I18n.WormStatusName, I18n.WormStatusDescription);
			registry.RegisterStatus(WormStatus);
		}
		{
			OxidationStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Oxidation",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF00FFAD)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF98FFF7)),
				OxidationSprite,
				affectedByTimestop: false
			);
			OxidationStatus.AddLocalisation(I18n.OxidationStatusName, I18n.OxidationStatusDescription);
			registry.RegisterStatus(OxidationStatus);
		}
	}
}
