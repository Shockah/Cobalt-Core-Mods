using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using daisyowl.text;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shockah.Kokoro;

internal sealed class Content
{
	private static readonly Regex InfoFaceRegex = new("face=\"(.*?)\"");
	private static readonly Regex InfoSizeRegex = new("size=(\\d+)");
	private static readonly Regex CommonLineHeightRegex = new("lineHeight=(\\d+)");
	private static readonly Regex CommonBaseRegex = new("base=(\\d+)");
	private static readonly Regex CharIdRegex = new("id=(\\d+)");
	private static readonly Regex CharXRegex = new("x=(\\d+)");
	private static readonly Regex CharYRegex = new("y=(\\d+)");
	private static readonly Regex CharWidthRegex = new("width=(\\d+)");
	private static readonly Regex CharHeightRegex = new("height=(\\d+)");
	private static readonly Regex CharXOffsetRegex = new("xoffset=(\\-?\\d+)");
	private static readonly Regex CharYOffsetRegex = new("yoffset=(\\-?\\d+)");
	private static readonly Regex CharXAdvanceRegex = new("xadvance=(\\-?\\d+)");

	private static ModEntry Instance => ModEntry.Instance;

	internal ExternalSprite WormSprite { get; private set; } = null!;
	internal ExternalStatus WormStatus { get; private set; } = null!;

	internal ExternalSprite OxidationSprite { get; private set; } = null!;
	internal ExternalStatus OxidationStatus { get; private set; } = null!;

	internal ExternalSprite RedrawButtonSprite { get; private set; } = null!;
	internal ExternalSprite RedrawButtonOnSprite { get; private set; } = null!;
	internal ExternalSprite RedrawSprite { get; private set; } = null!;
	internal ExternalStatus RedrawStatus { get; private set; } = null!;

	internal ExternalSprite QuestionMarkSprite { get; private set; } = null!;
	internal ExternalSprite EqualSprite { get; private set; } = null!;
	internal ExternalSprite NotEqualSprite { get; private set; } = null!;
	internal ExternalSprite GreaterThanSprite { get; private set; } = null!;
	internal ExternalSprite LessThanSprite { get; private set; } = null!;
	internal ExternalSprite GreaterThanOrEqualSprite { get; private set; } = null!;
	internal ExternalSprite LessThanOrEqualSprite { get; private set; } = null!;

	internal ExternalSprite EnergySprite { get; private set; } = null!;
	internal ExternalSprite EnergyCostSatisfiedSprite { get; private set; } = null!;
	internal ExternalSprite EnergyCostUnsatisfiedSprite { get; private set; } = null!;

	internal ExternalSprite ContinueSprite { get; private set; } = null!;
	internal ExternalSprite StopSprite { get; private set; } = null!;

	internal ExternalSprite TempShieldNextTurnSprite { get; private set; } = null!;
	internal ExternalStatus TempShieldNextTurnStatus { get; private set; } = null!;
	internal ExternalSprite ShieldNextTurnSprite { get; private set; } = null!;
	internal ExternalStatus ShieldNextTurnStatus { get; private set; } = null!;

	internal Font PinchCompactFont { get; private set; } = null!;

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
		RedrawButtonSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.RedrawButton",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "RedrawButton.png"))
		);
		RedrawButtonOnSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.RedrawButtonOn",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "RedrawButtonOn.png"))
		);
		RedrawSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.Redraw",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "RedrawStatus.png"))
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
		EnergyCostSatisfiedSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.EnergyCostSatisfied",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "EnergyCostSatisfied.png"))
		);
		EnergyCostUnsatisfiedSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.EnergyCostUnsatisfied",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "EnergyCostUnsatisfied.png"))
		);
		ContinueSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Continue",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Continue.png"))
		);
		StopSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Stop",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Stop.png"))
		);
		TempShieldNextTurnSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.TempShieldNextTurn",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "TempShieldNextTurn.png"))
		);
		ShieldNextTurnSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Status.ShieldNextTurn",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "ShieldNextTurn.png"))
		);

		PinchCompactFont = LoadFont(
			registry: registry,
			id: $"{typeof(ModEntry).Namespace}.PinchCompactFont",
			atlasFile: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Fonts", "PinchCompact_0.png")),
			fntFile: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Fonts", "PinchCompact.fnt"))
		);
		PinchCompactFont.outlined = LoadFont(
			registry: registry,
			id: $"{typeof(ModEntry).Namespace}.PinchCompactOutlineFont",
			atlasFile: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Fonts", "PinchCompactOutline_0.png")),
			fntFile: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Fonts", "PinchCompactOutline.fnt")),
			hasOutline: true
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
		{
			RedrawStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Redraw",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFFF0000)),
				borderColor: null,
				RedrawSprite,
				affectedByTimestop: false
			);
			RedrawStatus.AddLocalisation(I18n.RedrawStatusName, I18n.RedrawStatusDescription);
			registry.RegisterStatus(RedrawStatus);
		}
		{
			TempShieldNextTurnStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.TempShieldNextTurn",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFB500BE)),
				borderColor: null,
				TempShieldNextTurnSprite,
				affectedByTimestop: false
			);
			TempShieldNextTurnStatus.AddLocalisation(I18n.TempShieldNextTurnStatusName, I18n.TempShieldNextTurnStatusDescription);
			registry.RegisterStatus(TempShieldNextTurnStatus);
		}
		{
			ShieldNextTurnStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.ShieldNextTurn",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF9FD0FF)),
				borderColor: null,
				ShieldNextTurnSprite,
				affectedByTimestop: false
			);
			ShieldNextTurnStatus.AddLocalisation(I18n.ShieldNextTurnStatusName, I18n.ShieldNextTurnStatusDescription);
			registry.RegisterStatus(ShieldNextTurnStatus);
		}
	}

	private static Font LoadFont(ISpriteRegistry registry, string id, FileInfo atlasFile, FileInfo fntFile, bool hasOutline = false)
	{
		var atlas = registry.RegisterArtOrThrow(id: id, file: atlasFile);
		var fntText = File.ReadAllText(fntFile.FullName).Split("\n");
		return new()
		{
			atlas = (Spr)atlas.Id!.Value,
			metrics = ParseFontMetrics(fntText, hasOutline)
		};
	}

	private static FontMetrics ParseFontMetrics(string[] lines, bool hasOutline = false)
	{
		string? face = null;
		int? size = null, lineHeight = null, @base = null;
		List<int> charsId = [], charsX = [], charsY = [], charsW = [], charsH = [], charsXOffset = [], charsYOffset = [], charsAdvance = [];
		int charId, charX, charY, charW, charH, charXOffset, charYOffset, charAdvance;
		Match match;

		foreach (var line in lines)
		{
			if (line.StartsWith("info "))
			{
				match = InfoFaceRegex.Match(line);
				if (match.Success)
					face = match.Groups[1].Value;

				match = InfoSizeRegex.Match(line);
				if (match.Success)
					size = int.Parse(match.Groups[1].Value);
			}
			else if (line.StartsWith("common "))
			{
				match = CommonLineHeightRegex.Match(line);
				if (match.Success)
					lineHeight = int.Parse(match.Groups[1].Value);

				match = CommonBaseRegex.Match(line);
				if (match.Success)
					@base = int.Parse(match.Groups[1].Value);
			}
			else if (line.StartsWith("char "))
			{
				match = CharIdRegex.Match(line);
				if (!match.Success)
					continue;
				charId = int.Parse(match.Groups[1].Value);

				match = CharXRegex.Match(line);
				if (!match.Success)
					continue;
				charX = int.Parse(match.Groups[1].Value) + (hasOutline ? 1 : 0);

				match = CharYRegex.Match(line);
				if (!match.Success)
					continue;
				charY = int.Parse(match.Groups[1].Value) + (hasOutline ? 1 : 0);

				match = CharWidthRegex.Match(line);
				if (!match.Success)
					continue;
				charW = int.Parse(match.Groups[1].Value) - (hasOutline ? 2 : 0);

				match = CharHeightRegex.Match(line);
				if (!match.Success)
					continue;
				charH = int.Parse(match.Groups[1].Value) - (hasOutline ? 2 : 0);

				match = CharXOffsetRegex.Match(line);
				if (!match.Success)
					continue;
				charXOffset = int.Parse(match.Groups[1].Value);

				match = CharYOffsetRegex.Match(line);
				if (!match.Success)
					continue;
				charYOffset = int.Parse(match.Groups[1].Value);

				match = CharXAdvanceRegex.Match(line);
				if (!match.Success)
					continue;
				charAdvance = int.Parse(match.Groups[1].Value) - 1;

				charsId.Add(charId);
				charsX.Add(charX);
				charsY.Add(charY);
				charsW.Add(charW);
				charsH.Add(charH);
				charsXOffset.Add(charXOffset);
				charsYOffset.Add(charYOffset);
				charsAdvance.Add(charAdvance);
			}
		}

		if (string.IsNullOrEmpty(face))
			throw new InvalidDataException("Invalid font metrics");
		if (size is not { } nonNullSize)
			throw new InvalidDataException("Invalid font metrics");
		if (lineHeight is not { } nonNullLineHeight)
			throw new InvalidDataException("Invalid font metrics");
		if (@base is not { } nonNullBase)
			throw new InvalidDataException("Invalid font metrics");

		return new()
		{
			name = face,
			size = nonNullSize,
			ascent = nonNullBase,
			descent = -(nonNullLineHeight - nonNullBase - 1),
			char_count = charsId.Count,
			chars = charsId.ToArray(),
			advance = charsAdvance.ToArray(),
			offset_x = charsXOffset.ToArray(),
			offset_y = Enumerable.Range(0, charsId.Count).Select(i => -nonNullBase + charsYOffset[i]).ToArray(),
			width = charsW.ToArray(),
			height = charsH.ToArray(),
			pack_x = charsX.ToArray(),
			pack_y = charsY.ToArray(),
			kerning = [],
			has_1px_outline = hasOutline,
		};
	}
}
