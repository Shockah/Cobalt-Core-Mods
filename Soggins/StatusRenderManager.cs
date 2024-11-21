using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

internal sealed class StatusRenderManager : IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	private static ModEntry Instance => ModEntry.Instance;

	internal StatusRenderManager()
	{
		Instance.KokoroApi.StatusRendering.RegisterHook(this, double.MinValue);
	}

	public IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(IKokoroApi.IV2.IStatusRenderingApi.IHook.IGetExtraStatusesToShowArgs args)
	{
		if (Instance.Api.GetSmug(args.State, args.Ship) is not null)
			yield return (Status: (Status)Instance.SmugStatus.Id!.Value, Priority: 10);
	}

	public (IReadOnlyList<Color> Colors, int? BarTickWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
	{
		if (args.Status != (Status)Instance.SmugStatus.Id!.Value && args.Status != (Status)Instance.DoubleTimeStatus.Id!.Value)
			return null;
		
		if (args.Status == (Status)Instance.DoubleTimeStatus.Id!.Value)
			return ([new(0, 0, 0, 0)], -3);

		var barCount = Instance.Api.GetMaxSmug(args.Ship) - Instance.Api.GetMinSmug(args.Ship) + 1;
		var colors = new Color[barCount];

		if (args.Ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0)
		{
			Array.Fill(colors, Colors.cheevoGold);
			return (colors, null);
		}

		var goodColor = Colors.cheevoGold;
		if (Instance.Api.IsOversmug(args.State, args.Ship))
		{
			var f = Math.Sin(MG.inst.g.time * Math.PI * 2) * 0.5 + 0.5;
			goodColor = Color.Lerp(Colors.downside, Colors.white, f);
		}

		for (var barIndex = 0; barIndex < colors.Length; barIndex++)
		{
			var smugIndex = barIndex + Instance.Api.GetMinSmug(args.Ship);
			if (smugIndex == 0)
			{
				colors[barIndex] = Colors.white;
				continue;
			}

			var smug = Instance.Api.GetSmug(args.State, args.Ship) ?? 0;
			if (smug < 0 && smugIndex >= smug && smugIndex < 0)
				colors[barIndex] = Colors.downside;
			else if (smug > 0 && smugIndex <= smug && smugIndex > 0)
				colors[barIndex] = goodColor;
			else
				colors[barIndex] = Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;
		}
		return (colors, null);
	}

	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == (Status)Instance.SmugStatus.Id!.Value)
		{
			if (args.Ship is not null)
			{
				if (args.Tooltips.FirstOrDefault() is TTGlossary)
					args.Tooltips[0] = new CustomTTGlossary(
						CustomTTGlossary.GlossaryType.status,
						() => I18n.SmugStatusName,
						() => I18n.SmugStatusLongDescription
					);

				if (MG.inst.g.state is { } state)
				{
					var botchChance = Math.Clamp(Instance.Api.GetSmugBotchChance(state, state.ship, null), 0, 1);
					var doubleChance = Math.Clamp(Instance.Api.GetSmugDoubleChance(state, state.ship, null), 0, 1 - botchChance);
					args.Tooltips.Add(new TTText(string.Format(I18n.SmugStatusOddsDescription, doubleChance * 100, botchChance * 100)));
				}
			}
		}
		else if (args.Status == (Status)Instance.BidingTimeStatus.Id!.Value)
		{
			args.Tooltips.Add(new TTGlossary($"status.{Instance.DoubleTimeStatus.Id!.Value}", args.Amount));
		}
		else if (args.Status == (Status)Instance.DoublersLuckStatus.Id!.Value)
		{
			args.Tooltips.Clear();
			args.Tooltips.Add(new TTGlossary($"status.{Instance.DoublersLuckStatus.Id!.Value}", args.Amount + 1));
		}
		return args.Tooltips;
	}
}
