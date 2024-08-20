using System;
using System.Collections.Generic;
using System.Linq;

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
		if (Instance.Api.GetSmug(state, ship) is not null)
			yield return (Status: (Status)Instance.SmugStatus.Id!.Value, Priority: 10);
	}

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

		var goodColor = Colors.cheevoGold;
		if (Instance.Api.IsOversmug(state, ship))
		{
			double f = Math.Sin(Instance.KokoroApi.TotalGameTime.TotalSeconds * Math.PI * 2) * 0.5 + 0.5;
			goodColor = Color.Lerp(Colors.downside, Colors.white, f);
		}

		for (int barIndex = 0; barIndex < colors.Length; barIndex++)
		{
			var smugIndex = barIndex + Instance.Api.GetMinSmug(ship);
			if (smugIndex == 0)
			{
				colors[barIndex] = Colors.white;
				continue;
			}

			var smug = Instance.Api.GetSmug(state, ship) ?? 0;
			if (smug < 0 && smugIndex >= smug && smugIndex < 0)
				colors[barIndex] = Colors.downside;
			else if (smug > 0 && smugIndex <= smug && smugIndex > 0)
				colors[barIndex] = goodColor;
			else
				colors[barIndex] = Instance.KokoroApi.DefaultInactiveStatusBarColor;
		}
		return (colors, null);
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips)
	{
		if (status == (Status)Instance.SmugStatus.Id!.Value)
		{
			if (isForShipStatus)
			{
				var glossary = tooltips.FirstOrDefault() as TTGlossary;
				if (glossary is not null)
					tooltips[0] = new CustomTTGlossary(
						CustomTTGlossary.GlossaryType.status,
						() => I18n.SmugStatusName,
						() => I18n.SmugStatusLongDescription
					);

				if (MG.inst.g.state is { } state)
				{
					double botchChance = Math.Clamp(Instance.Api.GetSmugBotchChance(state, state.ship, null), 0, 1);
					double doubleChance = Math.Clamp(Instance.Api.GetSmugDoubleChance(state, state.ship, null), 0, 1 - botchChance);
					tooltips.Add(new TTText(string.Format(I18n.SmugStatusOddsDescription, doubleChance * 100, botchChance * 100)));
				}
			}
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
