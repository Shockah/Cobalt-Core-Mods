using System;
using System.Collections.Generic;
using System.Linq;
using daisyowl.text;
using Shockah.Shared;

namespace Shockah.Dracula;

internal sealed class ActionChoiceRoute : Route
{
	private static readonly UK ChoiceKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	public required string Title;
	public List<List<CardAction>> Choices = [];
	public bool IsPreview;

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

		const int centerX = 240;
		const int topY = 44;

		const int columns = 3;
		const int choiceWidth = 61;
		const int choiceHeight = 25;
		const int choiceSpacing = 4;
		const int actionSpacing = 4;
		const int actionYOffset = 7;
		const int actionHoverYOffset = 1;

		var fullRowWidth = columns * choiceWidth + Math.Max(columns - 1, 0) * choiceSpacing;
		var choicesStartX = centerX - fullRowWidth / 2;

		SharedArt.DrawEngineering(g);

		Draw.Text(Title, centerX, topY, font: DB.stapler, color: Colors.textMain, align: TAlign.Center);

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

				var actionStartX = choiceStartX + choiceWidth / 2 - totalActionWidth / 2;
				var actionXOffset = 0;
				foreach (var action in choice)
				{
					g.Push(rect: new(actionStartX + actionXOffset, choiceTopY + actionYOffset + (buttonResult.isHover ? actionHoverYOffset : 0)));
					actionXOffset += Card.RenderAction(g, g.state, action, dontDraw: false) + actionSpacing;
					g.Pop();
				}
			}
		}

		if (IsPreview)
			SharedArt.ButtonText(
				g,
				new Vec(210, 230),
				StableUK.upgradeCard_cancel,
				Loc.T("uiShared.btnBack"),
				onMouseDown: new MouseDownHandler(() => g.CloseRoute(this)),
				platformButtonHint: Btn.B
			);
	}

	private void OnChoice(G g, List<CardAction> actions)
	{
		if (IsPreview)
			return;
		if (g.state.route is not Combat combat)
			return;
		
		combat.QueueImmediate(actions);
		g.CloseRoute(this);
	}
}
