using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ITimesPlayedApi TimesPlayed { get; } = new TimesPlayedApi();
		
		public sealed class TimesPlayedApi : IKokoroApi.IV2.ITimesPlayedApi
		{
			public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedVariableHintAction? AsVariableHintAction(CardAction action)
				=> action as IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedVariableHintAction;

			public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedVariableHintAction MakeVariableHintAction(int cardId, IKokoroApi.IV2.ITimesPlayedApi.Interval interval)
				=> new TimesPlayedVariableHint { CardId = cardId, Interval = interval };

			public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedConditionExpression? AsConditionExpression(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedConditionExpression;

			public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedConditionExpression MakeConditionExpression(State state, Combat combat, int cardId, IKokoroApi.IV2.ITimesPlayedApi.Interval interval)
				=> new TimesPlayedCondition { CardId = cardId, Interval = interval, CurrentTimesPlayed = state.FindCard(cardId) is { } card ? GetTimesPlayed(card, interval) : 0 };

			public int GetTimesPlayed(Card card, IKokoroApi.IV2.ITimesPlayedApi.Interval interval)
				=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(card, $"TimesPlayedThis{Enum.GetName(interval)}");

			public void SetTimesPlayed(Card card, IKokoroApi.IV2.ITimesPlayedApi.Interval interval, int value)
			{
				value = Math.Max(value, 0);
			
				if (value == 0)
					ModEntry.Instance.Helper.ModData.RemoveModData(card, $"TimesPlayedThis{interval}");
				else
					ModEntry.Instance.Helper.ModData.SetModData(card, $"TimesPlayedThis{interval}", value);
			}
		}
	}
}

internal sealed class TimesPlayedManager
{
	internal static ISpriteEntry IconRun { get; private set; } = null!;
	internal static ISpriteEntry IconCombat { get; private set; } = null!;
	internal static ISpriteEntry IconTurn { get; private set; } = null!;

	internal static void Setup(IHarmony harmony)
	{
		IconRun = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/TimesPlayedThisRun.png"));
		IconCombat = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/TimesPlayedThisCombat.png"));
		IconTurn = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/TimesPlayedThisTurn.png"));

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (State state) =>
		{
			foreach (var card in state.deck)
				ModEntry.Instance.Api.V2.TimesPlayed.SetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn, 0);
		}, 1000);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
			{
				ModEntry.Instance.Api.V2.TimesPlayed.SetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat, 0);
				ModEntry.Instance.Api.V2.TimesPlayed.SetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn, 0);
			}
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card) =>
		{
			ModEntry.Instance.Api.V2.TimesPlayed.SetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn, ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn) + 1);
			ModEntry.Instance.Api.V2.TimesPlayed.SetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat, ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1);
			ModEntry.Instance.Api.V2.TimesPlayed.SetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Run, ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Run) + 1);
		});
	}
}

internal sealed class TimesPlayedVariableHint : AVariableHint, IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedVariableHintAction
{
	public required int CardId { get; set; }
	public required IKokoroApi.IV2.ITimesPlayedApi.Interval Interval { get; set; }

	[JsonIgnore]
	public AVariableHint AsCardAction
		=> this;

	public TimesPlayedVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new()
		{
			path = Interval switch
			{
				IKokoroApi.IV2.ITimesPlayedApi.Interval.Run => TimesPlayedManager.IconRun.Sprite,
				IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat => TimesPlayedManager.IconCombat.Sprite,
				IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn => TimesPlayedManager.IconTurn.Sprite,
				_ => throw new ArgumentOutOfRangeException()
			}
		};

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.xHintTimesPlayedThis{Interval}.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(
					["x", "TimesPlayed", Interval.ToString(), s.route is Combat ? "stateful" : "stateless"],
					new { Count = (s.FindCard(CardId) is { } card ? ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, Interval) : 0) + 1 }
				)
			}
		];

	public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedVariableHintAction SetCardId(int value)
	{
		CardId = value;
		return this;
	}

	public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedVariableHintAction SetInterval(IKokoroApi.IV2.ITimesPlayedApi.Interval value)
	{
		Interval = value;
		return this;
	}
}

internal sealed class TimesPlayedCondition : IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedConditionExpression
{
	public required int CardId { get; internal set; }
	public required IKokoroApi.IV2.ITimesPlayedApi.Interval Interval { get; set; }
	public required int CurrentTimesPlayed { get; set; }

	public string GetTooltipDescription(State state, Combat combat)
		=> ModEntry.Instance.Localizations.Localize(["timesPlayed", "condition", Interval.ToString()]);

	public int GetValue(State state, Combat combat)
		=> CurrentTimesPlayed;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		var icon = Interval switch
		{
			IKokoroApi.IV2.ITimesPlayedApi.Interval.Run => TimesPlayedManager.IconRun,
			IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat => TimesPlayedManager.IconCombat,
			IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn => TimesPlayedManager.IconTurn,
			_ => throw new ArgumentOutOfRangeException()
		};
		
		if (!dontRender)
			Draw.Sprite(icon.Sprite, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}
	
	public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedConditionExpression SetCard(State state, Combat combat, int cardId, IKokoroApi.IV2.ITimesPlayedApi.Interval interval)
	{
		CardId = cardId;
		Interval = interval;
		if (state.FindCard(cardId) is { } card)
			CurrentTimesPlayed = ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, interval);
		return this;
	}
	
	public IKokoroApi.IV2.ITimesPlayedApi.ITimesPlayedConditionExpression SetCurrentTimesPlayed(int value)
	{
		CurrentTimesPlayed = value;
		return this;
	}
}