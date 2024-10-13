using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public CardAction MakeTimesPlayedVariableHintAction(int cardId)
			=> new TimesPlayedVariableHint { CardId = cardId };

		public IKokoroApi.IConditionalActionApi.IIntExpression MakeTimesPlayedCondition(int currentTimesPlayed)
			=> new TimesPlayedCondition { CurrentTimesPlayed = currentTimesPlayed };

		public int GetTimesPlayed(Card card)
			=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(card, "TimesPlayed");

		public void SetTimesPlayed(Card card, int value)
		{
			value = Math.Max(value, 0);
			
			if (value == 0)
				ModEntry.Instance.Helper.ModData.RemoveModData(card, "TimesPlayed");
			else
				ModEntry.Instance.Helper.ModData.SetModData(card, "TimesPlayed", value);
		}
	}
}

internal sealed class TimesPlayedManager
{
	internal static ISpriteEntry Icon { get; private set; } = null!;

	internal static void Setup(IHarmony harmony)
	{
		Icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/TimesPlayed.png"));

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
				ModEntry.Instance.Api.Actions.SetTimesPlayed(card, 0);
		}, 0);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card) =>
		{
			ModEntry.Instance.Api.Actions.SetTimesPlayed(card, ModEntry.Instance.Api.Actions.GetTimesPlayed(card) + 1);
		}, 0);
	}
}

internal sealed class TimesPlayedVariableHint : AVariableHint
{
	public required int CardId;

	public TimesPlayedVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new() { path = TimesPlayedManager.Icon.Sprite };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("action.xHintTimesPlayed.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(
					["x", "TimesPlayed", s.route is Combat ? "stateful" : "stateless"],
					new { Count = (s.FindCard(CardId) is { } card ? ModEntry.Instance.Api.Actions.GetTimesPlayed(card) : 0) + 1 }
				)
			}
		];
}

internal sealed class TimesPlayedCondition : IKokoroApi.IConditionalActionApi.IIntExpression
{
	public required int CurrentTimesPlayed;

	public string GetTooltipDescription(State state, Combat? combat)
		=> ModEntry.Instance.Localizations.Localize(["condition", "TimesPlayed"]);

	public int GetValue(State state, Combat combat)
		=> CurrentTimesPlayed;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(TimesPlayedManager.Icon.Sprite, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}
}