using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;
using Nickel;

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

	public (IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
	{
		if (args.Status != (Status)Instance.SmugStatus.Id!.Value && args.Status != (Status)Instance.DoubleTimeStatus.Id!.Value)
			return null;
		
		if (args.Status == (Status)Instance.DoubleTimeStatus.Id!.Value)
			return ([new(0, 0, 0, 0)], -3);

		var badColor = Colors.downside;
		var goodColor = Colors.cheevoGold;
		var neutralColor = Colors.white;

		var barCount = Instance.Api.GetMaxSmug(args.Ship) - Instance.Api.GetMinSmug(args.Ship) + 1;
		var colors = new Color[barCount];

		var hasDoubleTime = args.Ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0;

		if (hasDoubleTime)
		{
			badColor = goodColor;
			neutralColor = goodColor;
		}
		if (Instance.Api.IsOversmug(args.State, args.Ship))
		{
			var f = Math.Sin(MG.inst.g.time * Math.PI * 2) * 0.5 + 0.5;
			goodColor = Color.Lerp(hasDoubleTime ? goodColor : badColor, Colors.white, f);
			if (hasDoubleTime)
				neutralColor = goodColor;
		}

		for (var barIndex = 0; barIndex < colors.Length; barIndex++)
		{
			var smugIndex = barIndex + Instance.Api.GetMinSmug(args.Ship);
			if (smugIndex == 0)
			{
				colors[barIndex] = neutralColor;
				continue;
			}

			var smug = Instance.Api.GetSmug(args.State, args.Ship) ?? 0;
			if (smug < 0 && smugIndex >= smug && smugIndex < 0)
				colors[barIndex] = badColor;
			else if (smug > 0 && smugIndex <= smug && smugIndex > 0)
				colors[barIndex] = goodColor;
			else
				colors[barIndex] = Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;
		}
		return (colors, null);
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		var newTooltips = args.Tooltips.ToList();
		
		if (args.Status == (Status)Instance.SmugStatus.Id!.Value)
		{
			if (args.Ship is not null)
			{
				if (newTooltips.FirstOrDefault() is TTGlossary glossary)
					newTooltips[0] = new GlossaryTooltip(glossary.key)
					{
						Icon = (Spr)ModEntry.Instance.SmugStatusSprite.Id!.Value,
						TitleColor = Colors.status,
						Title = I18n.SmugStatusName,
						Description = I18n.SmugStatusLongDescription,
					};

				if (MG.inst.g.state is { } state)
				{
					var botchChance = Math.Clamp(Instance.Api.GetSmugBotchChance(state, state.ship, null), 0, 1);
					var doubleChance = Math.Clamp(Instance.Api.GetSmugDoubleChance(state, state.ship, null), 0, 1 - botchChance);
					newTooltips.Add(new TTText(string.Format(I18n.SmugStatusOddsDescription, doubleChance * 100, botchChance * 100)));
				}
			}
		}
		else if (args.Status == (Status)Instance.BidingTimeStatus.Id!.Value)
		{
			newTooltips.Add(new TTGlossary($"status.{Instance.DoubleTimeStatus.Id!.Value}", args.Amount));
		}
		else if (args.Status == (Status)Instance.DoublersLuckStatus.Id!.Value)
		{
			newTooltips.Clear();
			newTooltips.Add(new TTGlossary($"status.{Instance.DoublersLuckStatus.Id!.Value}", args.Amount + 1));
		}
		return newTooltips;
	}
}
