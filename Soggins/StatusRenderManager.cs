using System;
using System.Collections.Generic;

namespace Shockah.Soggins;

internal sealed class StatusRenderManager : IStatusRenderHook
{
	private static ModEntry Instance => ModEntry.Instance;

	internal StatusRenderManager() : base()
	{
		Instance.KokoroApi.RegisterStatusRenderHook(this, double.MinValue);
	}

	public IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(State state, Combat combat, Ship ship)
	{
		if (Instance.Api.GetSmug(ship) is not null)
			yield return (Status: (Status)Instance.SmugStatus.Id!.Value, Priority: 10);
	}

	public bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount)
		=> status == (Status)Instance.SmuggedStatus.Id!.Value ? false : null;

	public bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount)
		=> status == (Status)Instance.SmugStatus.Id!.Value || status == (Status)Instance.DoubleTimeStatus.Id!.Value ? true : null;

	public (IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount)
	{
		if (status == (Status)Instance.DoubleTimeStatus.Id!.Value)
			return (new Color[] { new(0, 0, 0, 0) }, -3);

		var barCount = Instance.Api.GetMaxSmug(ship) - Instance.Api.GetMinSmug(ship) + 1;
		var colors = new Color[barCount];

		if (ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0)
		{
			Array.Fill(colors, Colors.cheevoGold);
			return (colors, null);
		}
		if (Instance.Api.IsOversmug(ship))
		{
			Array.Fill(colors, Colors.downside);
			return (colors, null);
		}

		for (int barIndex = 0; barIndex < colors.Length; barIndex++)
		{
			var smugIndex = barIndex + Instance.Api.GetMinSmug(ship);
			if (smugIndex == 0)
			{
				colors[barIndex] = Colors.white;
				continue;
			}

			var smug = Instance.Api.GetSmug(ship) ?? 0;
			if (smug < 0 && smugIndex >= smug && smugIndex < 0)
				colors[barIndex] = Colors.downside;
			else if (smug > 0 && smugIndex <= smug && smugIndex > 0)
				colors[barIndex] = Colors.cheevoGold;
			else
				colors[barIndex] = Instance.KokoroApi.DefaultInactiveStatusBarColor;
		}
		return (colors, null);
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips)
	{
		if (status == (Status)Instance.SmugStatus.Id!.Value)
		{
			if (isForShipStatus && StateExt.Instance is { } state && state.ship.Get((Status)Instance.SmuggedStatus.Id!.Value) > 0)
				tooltips.Add(new TTText(string.Format(I18n.SmugStatusCurrentChancesDescription, Instance.Api.GetSmugDoubleChance(state, state.ship, null) * 100, Instance.Api.GetSmugBotchChance(state, state.ship, null) * 100)));
		}
		else if (status == (Status)Instance.BidingTimeStatus.Id!.Value)
		{
			tooltips.Add(new TTGlossary($"status.{Instance.DoubleTimeStatus.Id!.Value}", amount));
		}
		else if (status == (Status)Instance.DoublersLuckStatus.Id!.Value)
		{
			tooltips.Clear();
			tooltips.Add(new TTGlossary($"status.{Instance.DoublersLuckStatus.Id!.Value}", amount + 1));
		}
		return tooltips;
	}
}
