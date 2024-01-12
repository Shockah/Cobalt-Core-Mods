using daisyowl.text;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

internal sealed class ActionChoiceRoute : Route
{
	private const UK ChoiceKey = (UK)2137021;

	public string? Title;

	public List<List<CardAction>> Choices = [];

	public override bool GetShowOverworldPanels()
		=> true;

	public override bool CanBePeeked()
		=> true;

	public override void Render(G g)
	{
		base.Render(g);
		if (Choices.Count == 0)
		{
			g.CloseRoute(this);
			return;
		}

		int centerX = 240;
		int topY = 44;

		int columns = 3;
		int choiceWidth = 56;
		int choiceHeight = 18;
		int choiceSpacing = 4;
		int actionSpacing = 4;
		int actionYOffset = 7;
		int actionHoverYOffset = 1;

		int fullRowWidth = columns * choiceWidth + Math.Max(columns - 1, 0) * choiceSpacing;
		int choicesStartX = centerX - fullRowWidth / 2;

		SharedArt.DrawEngineering(g);

		Draw.Text(Title ?? "PICK A CHOICE", centerX, topY, font: DB.stapler, color: Colors.textMain, align: TAlign.Center);

		var rows = Choices.Chunk(columns).ToList();
		for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
		{
			var row = rows[rowIndex];
			var rowWidth = row.Length * choiceWidth + Math.Max(row.Length - 1, 0) * choiceSpacing;
			var rowStartX = choicesStartX + (fullRowWidth - rowWidth) / 2;

			for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
			{
				var choice = row[columnIndex];
				var choiceStartX = rowStartX + (choiceWidth + choiceSpacing) * columnIndex;
				var choiceTopY = topY + 24 + rowIndex * (choiceHeight + choiceSpacing);

				var buttonRect = new Rect(choiceStartX, choiceTopY, choiceWidth, choiceHeight);
				var buttonResult = SharedArt.ButtonText(
					g, Vec.Zero, new UIKey(ChoiceKey, rowIndex * columns + columnIndex), "", rect: buttonRect,
					onMouseDown: new MouseDownHandler(() => OnChoice(g, choice))
				);

				if (buttonResult.isHover)
					g.tooltips.Add(new Vec(buttonRect.x + buttonRect.w, buttonRect.y + buttonRect.h), choice.SelectMany(c => c.GetTooltips(g.state)));

				var totalActionWidth = 0;
				foreach (var action in choice)
				{
					if (totalActionWidth != 0)
						totalActionWidth += actionSpacing;
					totalActionWidth += Card.RenderAction(g, g.state, action, dontDraw: true);
				}

				var actionStartX = choiceStartX + 3 + choiceWidth / 2 - totalActionWidth / 2;
				var actionXOffset = 0;
				foreach (var action in choice)
				{
					g.Push(rect: new(actionStartX + actionXOffset, choiceTopY + actionYOffset + (buttonResult.isHover ? actionHoverYOffset : 0)));
					actionXOffset += Card.RenderAction(g, g.state, action, dontDraw: false) + actionSpacing;
					g.Pop();
				}
			}
		}
	}

	private void OnChoice(G g, List<CardAction> actions)
	{
		if (g.state.route is not Combat combat)
			return;
		combat.Queue(new ADelay());
		foreach (var action in actions)
			combat.Queue(action);
		g.CloseRoute(this);
	}
}
