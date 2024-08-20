using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Wormholes;

internal static class MapBasePatches
{
	private enum Rows
	{
		FirstToLast, LastToFirst, Random
	}

	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(MapBase), nameof(MapBase.Populate)),
			postfix: new HarmonyMethod(typeof(MapBasePatches), nameof(MapBase_Populate_Postfix))
		);
	}

	private static void MapBase_Populate_Postfix(MapBase __instance, Rand rng)
	{
		int firstY = __instance.markers.Keys.Min(v => (int)v.y);
		int lastY = __instance.markers.Keys.Max(v => (int)v.y);
		int minX = __instance.markers.Keys.Min(v => (int)v.x);
		int maxX = __instance.markers.Keys.Max(v => (int)v.x);

		int firstPossibleY = firstY + 1;
		int lastPossibleY = lastY - (__instance.IsFinalZone() ? 2 : 3);
		var preferredSkipLength = (int)Math.Ceiling((lastPossibleY - firstPossibleY + 1) / 2.0);

		IEnumerable<Vec> GetRandomPositionEnumerable(int minY, int maxY, int minX, int maxX, Rows rows, int maxPathOffset)
		{
			var rowsEnumerable = Enumerable.Range(minY, maxY - minY + 1);
			switch (rows)
			{
				case Rows.FirstToLast:
					break;
				case Rows.LastToFirst:
					rowsEnumerable = rowsEnumerable.Reverse();
					break;
				case Rows.Random:
					rowsEnumerable = rowsEnumerable.Shuffle(rng);
					break;
			}

			foreach (var y in rowsEnumerable)
			{
				foreach (var x in Enumerable.Range(minX, maxX - minX + 1).Shuffle(rng))
				{
					if (__instance.markers.TryGetValue(new(x, y), out var marker) && marker.contents is not null)
						continue;
					if (x == minX && (!__instance.markers.TryGetValue(new(x + 1, y), out var neighbor) || neighbor.contents is null))
						continue;
					if (x == maxX && (!__instance.markers.TryGetValue(new(x - 1, y), out neighbor) || neighbor.contents is null))
						continue;

					bool HasPossiblePath(int offsetX, int offsetY)
					{
						if (!__instance.markers.TryGetValue(new(x + offsetX, y + offsetY), out var marker))
							return false;
						return marker.contents is not null;
					}

					if (!Enumerable.Range(-maxPathOffset, maxPathOffset * 2 + 1).Any(offset => HasPossiblePath(offset, -1) && HasPossiblePath(offset, 1)))
						continue;
					yield return new(x, y);
				}
			}
		}

		IEnumerable<(Vec EarlyPosition, Vec LatePosition)> GetRandomPositionsEnumerable(int minX, int maxX, int maxPathOffset = 1)
		{
			foreach (var lateWormholePosition in GetRandomPositionEnumerable(lastPossibleY - 2, lastPossibleY, minX, maxX, Rows.LastToFirst, maxPathOffset))
				foreach (var earlyWormholePosition in GetRandomPositionEnumerable(firstPossibleY, Math.Max((int)lateWormholePosition.y - 4, firstPossibleY), minX, maxX, Rows.Random, maxPathOffset).OrderByDescending(p => Math.Abs(p.x - lateWormholePosition.x)))
					if (Math.Abs(lateWormholePosition.x - earlyWormholePosition.x) <= 6)
						yield return (earlyWormholePosition, lateWormholePosition);
		}

		var positions = GetRandomPositionsEnumerable(minX - 1, maxX + 1, maxPathOffset: 2)
			.Where(p => Math.Abs((int)p.EarlyPosition.x - (int)p.LatePosition.x) <= 6)
			.Select(p =>
			{
				int GetMinXOffset(Vec position, int offsetY)
				{
					bool HasPossiblePath(int offsetX, int offsetY)
					{
						if (!__instance.markers.TryGetValue(new(position.x + offsetX, position.y + offsetY), out var marker))
							return false;
						return marker.contents is not null;
					}

					for (int i = 0; i <= 2; i++)
					{
						if (i != 0 && HasPossiblePath(-i, -1) && HasPossiblePath(-i, 1))
							return i;
						if (HasPossiblePath(i, -1) && HasPossiblePath(i, 1))
							return i;
					}
					return 100;
				}

				return (
					EarlyPosition: p.EarlyPosition,
					LatePosition: p.LatePosition,
					EarlyMinPreviousXOffset: Math.Max(GetMinXOffset(p.EarlyPosition, -1), 1),
					EarlyMinNextXOffset: Math.Max(GetMinXOffset(p.EarlyPosition, 1), 1),
					LateMinPreviousXOffset: Math.Max(GetMinXOffset(p.LatePosition, -1), 1),
					LateMinNextXOffset: Math.Max(GetMinXOffset(p.LatePosition, 1), 1)
				);
			})
			.OrderBy(p => Math.Max(Math.Max(p.EarlyMinPreviousXOffset, p.EarlyMinNextXOffset), Math.Max(p.LateMinPreviousXOffset, p.LateMinNextXOffset)))
			.ThenBy(p => Math.Abs(Math.Max(maxX, Math.Max((int)p.EarlyPosition.x, (int)p.LatePosition.x)) - Math.Min(minX, Math.Min((int)p.EarlyPosition.x, (int)p.LatePosition.x))))
			.ThenBy(p => Math.Max(p.EarlyMinPreviousXOffset, p.EarlyMinNextXOffset) + Math.Max(p.LateMinPreviousXOffset, p.LateMinNextXOffset))
			.ThenBy(p => p.EarlyMinPreviousXOffset + p.EarlyMinNextXOffset + p.LateMinPreviousXOffset + p.LateMinNextXOffset)
			.ThenBy(p => Math.Abs(Math.Abs((int)p.EarlyPosition.y - (int)p.LatePosition.y) - 1 - preferredSkipLength))
			.ThenByDescending(p => Math.Abs((int)p.EarlyPosition.x - (int)p.LatePosition.x))
			.FirstOrNull();
		if (positions is null)
			return;

		if (!__instance.markers.TryGetValue(positions.Value.EarlyPosition, out var earlyWormhole))
		{
			earlyWormhole = new();
			__instance.markers[positions.Value.EarlyPosition] = earlyWormhole;
		}
		if (!__instance.markers.TryGetValue(positions.Value.LatePosition, out var lateWormhole))
		{
			lateWormhole = new();
			__instance.markers[positions.Value.LatePosition] = lateWormhole;
		}

		earlyWormhole.contents = new MapWormhole { OtherWormholePosition = positions.Value.LatePosition, IsFurther = false };
		lateWormhole.contents = new MapWormhole { OtherWormholePosition = positions.Value.EarlyPosition, IsFurther = true };

		void AddPaths(Marker marker, Vec position, int minPreviousXOffset, int minNextXOffset)
		{
			for (int span = 0; span <= Math.Max(minPreviousXOffset, 1); span++)
			{
				bool addedAny = false;
				if (span != 0 && __instance.markers.TryGetValue(new(position.x - span, position.y - 1), out var neighbor) && neighbor.contents is not null)
				{
					neighbor.paths.Add((int)position.x);
					addedAny = true;
				}
				if (__instance.markers.TryGetValue(new(position.x + span, position.y - 1), out neighbor) && neighbor.contents is not null)
				{
					neighbor.paths.Add((int)position.x);
					addedAny = true;
				}
				if (span >= 1 && addedAny)
					break;
			}

			for (int span = 0; span <= Math.Max(minNextXOffset, 1); span++)
			{
				bool addedAny = false;
				if (span != 0 && __instance.markers.TryGetValue(new(position.x - span, position.y + 1), out var neighbor) && neighbor.contents is not null)
				{
					marker.paths.Add((int)position.x - span);
					addedAny = true;
				}
				if (__instance.markers.TryGetValue(new(position.x + span, position.y + 1), out neighbor) && neighbor.contents is not null)
				{
					marker.paths.Add((int)position.x + span);
					addedAny = true;
				}
				if (span >= 1 && addedAny)
					break;
			}
		}

		AddPaths(earlyWormhole, positions.Value.EarlyPosition, positions.Value.EarlyMinPreviousXOffset, positions.Value.EarlyMinNextXOffset);
		AddPaths(lateWormhole, positions.Value.LatePosition, positions.Value.LateMinPreviousXOffset, positions.Value.LateMinNextXOffset);
	}
}
