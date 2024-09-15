using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public IKokoroApi.IConditionalActionApi ConditionalActions { get; } = new ConditionalActionApiImplementation();

	public sealed class ConditionalActionApiImplementation : IKokoroApi.IConditionalActionApi
	{
		public CardAction Make(IKokoroApi.IConditionalActionApi.IBoolExpression expression, CardAction action, bool fadeUnsatisfied = true)
			=> new AConditional { Expression = expression, Action = action, FadeUnsatisfied = fadeUnsatisfied };

		public IKokoroApi.IConditionalActionApi.IIntExpression Constant(int value)
			=> new ConditionalActionIntConstant(value);

		public IKokoroApi.IConditionalActionApi.IIntExpression HandConstant(int value)
			=> new ConditionalActionHandConstant(value);

		public IKokoroApi.IConditionalActionApi.IIntExpression XConstant(int value)
			=> new ConditionalActionXConstant(value);

		public IKokoroApi.IConditionalActionApi.IIntExpression ScalarMultiplier(IKokoroApi.IConditionalActionApi.IIntExpression expression, int scalar)
			=> new ConditionalActionScalarMultiplier(expression, scalar);

		public IKokoroApi.IConditionalActionApi.IBoolExpression HasStatus(Status status, bool targetPlayer = true, bool countNegative = false)
			=> new ConditionalActionHasStatusExpression(status, targetPlayer, countNegative);

		public IKokoroApi.IConditionalActionApi.IIntExpression Status(Status status, bool targetPlayer = true)
			=> new ConditionalActionStatusExpression(status, targetPlayer);

		public IKokoroApi.IConditionalActionApi.IBoolExpression Equation(
			IKokoroApi.IConditionalActionApi.IIntExpression lhs,
			IKokoroApi.IConditionalActionApi.EquationOperator @operator,
			IKokoroApi.IConditionalActionApi.IIntExpression rhs,
			IKokoroApi.IConditionalActionApi.EquationStyle style,
			bool hideOperator = false
		)
			=> new ConditionalActionEquation(lhs, @operator, rhs, style, hideOperator);
	}
}

internal sealed class ConditionalActionManager : IWrappedActionHook
{
	internal static readonly ConditionalActionManager Instance = new();
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	internal static void SetupLate()
		=> WrappedActionManager.Instance.Register(Instance, 0);
	
	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AConditional conditional)
			return null;
		if (conditional.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
	
	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AConditional conditional)
			return true;
		if (conditional.Action is not { } wrappedAction)
			return false;

		var oldActionDisabled = wrappedAction.disabled;
		var faded = action.disabled || (conditional.FadeUnsatisfied && state.route is Combat combat && conditional.Expression?.GetValue(state, combat) == false);
		wrappedAction.disabled = faded;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		conditional.Expression?.Render(g, ref position, faded, dontDraw);
		if (conditional.Expression?.ShouldRenderQuestionMark(state, state.route as Combat) == true)
		{
			if (!dontDraw)
				Draw.Sprite((Spr)ModEntry.Instance.Content.QuestionMarkSprite.Id!.Value, position.x, position.y, color: faded ? Colors.disabledIconTint : Colors.white);
			position.x += SpriteLoader.Get((Spr)ModEntry.Instance.Content.QuestionMarkSprite.Id!.Value)?.Width ?? 0;
			position.x -= 1;
		}

		position.x += 2;
		if (wrappedAction is AAttack attack)
		{
			var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
			if (shouldStun)
				attack.stunEnemy = shouldStun;
		}

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		wrappedAction.disabled = oldActionDisabled;

		return false;
	}
}

public sealed class AConditional : CardAction
{
	public IKokoroApi.IConditionalActionApi.IBoolExpression? Expression;
	public CardAction? Action;
	public bool FadeUnsatisfied = true;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Expression is null || Action is null)
			return;
		if (!Expression.GetValue(s, c))
			return;

		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = [];
		if (Expression is not null)
		{
			var description = Expression.GetTooltipDescription(s, s.route as Combat);
			var formattedDescription = string.Format(ModEntry.Instance.Localizations.Localize(["conditional", "description"]), description);
			var defaultTooltip = new GlossaryTooltip($"AConditional::{formattedDescription}")
			{
				Icon = (Spr)ModEntry.Instance.Content.QuestionMarkSprite.Id!.Value,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["conditional", "name"]),
				Description = formattedDescription
			};

			tooltips.AddRange(Expression.OverrideConditionalTooltip(s, s.route as Combat, defaultTooltip, formattedDescription));
			tooltips.AddRange(Expression.GetTooltips(s, s.route as Combat));
		}
		if (Action is not null && !Action.omitFromTooltips)
			tooltips.AddRange(Action.GetTooltips(s));
		return tooltips;
	}
}

internal sealed class ConditionalActionIntConstant : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly int Value;

	[JsonConstructor]
	public ConditionalActionIntConstant(int value)
	{
		this.Value = value;
	}

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			BigNumbers.Render(Value, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Value}".Length * 6;
	}

	public string GetTooltipDescription(State state, Combat? combat)
		=> $"<c=boldPink>{Value}</c>";
}

internal sealed class ConditionalActionHandConstant : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly int Value;

	[JsonConstructor]
	public ConditionalActionHandConstant(int value)
	{
		this.Value = value;
	}

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(StableSpr.icons_dest_hand, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public string GetTooltipDescription(State state, Combat? combat)
		=> ModEntry.Instance.Localizations.Localize(["conditional", "hand"]);
}

internal sealed class ConditionalActionXConstant : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly int Value;

	[JsonConstructor]
	public ConditionalActionXConstant(int value)
	{
		this.Value = value;
	}

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(StableSpr.icons_x, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public string GetTooltipDescription(State state, Combat? combat)
		=> ModEntry.Instance.Localizations.Localize(["conditional", "x"]);
}

internal sealed class ConditionalActionScalarMultiplier : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly IKokoroApi.IConditionalActionApi.IIntExpression Expression;

	[JsonProperty]
	public readonly int Scalar;

	[JsonIgnore]
	internal int? ConstantValue
		=> this.Expression switch
		{
			ConditionalActionIntConstant constant => constant.Value,
			ConditionalActionScalarMultiplier scalarMultiplier => scalarMultiplier.ConstantValue * this.Scalar,
			_ => null
		};

	[JsonConstructor]
	public ConditionalActionScalarMultiplier(IKokoroApi.IConditionalActionApi.IIntExpression expression, int scalar)
	{
		this.Expression = expression;
		this.Scalar = scalar;
	}

	public int GetValue(State state, Combat combat)
		=> Expression.GetValue(state, combat) * Scalar;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		var constantValue = ConstantValue;
		if (constantValue is not null)
		{
			new ConditionalActionIntConstant(constantValue.Value).Render(g, ref position, isDisabled, dontRender);
			return;
		}

		if (!dontRender)
			BigNumbers.Render(Scalar, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Scalar}".Length * 6;
		Expression.Render(g, ref position, isDisabled, dontRender);
	}

	public string GetTooltipDescription(State state, Combat? combat)
	{
		var constantValue = ConstantValue;
		if (constantValue is not null)
			return new ConditionalActionIntConstant(constantValue.Value).GetTooltipDescription(state, combat);
		return $"<c=boldPink>{Scalar}</c>x {Expression.GetTooltipDescription(state, combat)}";
	}
}

internal sealed class ConditionalActionHasStatusExpression : IKokoroApi.IConditionalActionApi.IBoolExpression
{
	[JsonProperty]
	public readonly Status Status;

	[JsonProperty]
	public readonly bool TargetPlayer;

	[JsonProperty]
	public readonly bool CountNegative;

	[JsonConstructor]
	public ConditionalActionHasStatusExpression(Status status, bool targetPlayer, bool countNegative)
	{
		this.Status = status;
		this.TargetPlayer = targetPlayer;
		this.CountNegative = countNegative;
	}

	public bool GetValue(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		var amount = ship.Get(Status);
		return CountNegative ? amount != 0 : amount > 0;
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!TargetPlayer)
		{
			if (!dontRender)
				Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 8;
		}

		var icon = DB.statuses[Status].icon;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public string GetTooltipDescription(State state, Combat? combat)
		=> string.Format(ModEntry.Instance.Localizations.Localize(["conditional", "hasStatus", this.TargetPlayer ? "player" : "enemy"]), $"<c=status>{Status.GetLocName().ToUpper()}</c>");

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		var ship = TargetPlayer ? state.ship : combat?.otherShip;
		var amount = ship?.Get(Status) ?? 1;
		return [new TTGlossary($"status.{Status.Key()}", amount)];
	}
}

internal sealed class ConditionalActionStatusExpression : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly Status Status;

	[JsonProperty]
	public readonly bool TargetPlayer;

	[JsonConstructor]
	public ConditionalActionStatusExpression(Status status, bool targetPlayer)
	{
		this.Status = status;
		this.TargetPlayer = targetPlayer;
	}

	public int GetValue(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		return ship.Get(Status);
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!TargetPlayer)
		{
			if (!dontRender)
				Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 8;
		}

		var icon = DB.statuses[Status].icon;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public string GetTooltipDescription(State state, Combat? combat)
		=> $"<c=status>{Status.GetLocName().ToUpper()}</c>";

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		var ship = TargetPlayer ? state.ship : combat?.otherShip;
		var amount = ship?.Get(Status) ?? 1;
		return [new TTGlossary($"status.{this.Status.Key()}", amount)];
	}
}

internal sealed class ConditionalActionEquation : IKokoroApi.IConditionalActionApi.IBoolExpression
{
	[JsonProperty]
	public readonly IKokoroApi.IConditionalActionApi.IIntExpression Lhs;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly IKokoroApi.IConditionalActionApi.EquationOperator Operator;

	[JsonProperty]
	public readonly IKokoroApi.IConditionalActionApi.IIntExpression Rhs;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly IKokoroApi.IConditionalActionApi.EquationStyle Style;

	[JsonProperty]
	public readonly bool HideOperator;

	[JsonConstructor]
	public ConditionalActionEquation(
		IKokoroApi.IConditionalActionApi.IIntExpression lhs,
		IKokoroApi.IConditionalActionApi.EquationOperator @operator,
		IKokoroApi.IConditionalActionApi.IIntExpression rhs,
		IKokoroApi.IConditionalActionApi.EquationStyle style,
		bool hideOperator
	)
	{
		this.Lhs = lhs;
		this.Operator = @operator;
		this.Rhs = rhs;
		this.Style = style;
		this.HideOperator = hideOperator;
	}

	public bool GetValue(State state, Combat combat)
	{
		var lhs = Lhs.GetValue(state, combat);
		var rhs = Rhs.GetValue(state, combat);
		return Operator switch
		{
			IKokoroApi.IConditionalActionApi.EquationOperator.Equal => lhs == rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => lhs != rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => lhs > rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => lhs < rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => lhs >= rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => lhs <= rhs,
			_ => throw new ArgumentException()
		};
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		Lhs.Render(g, ref position, isDisabled, dontRender);

		if (!HideOperator)
		{
			var operatorIcon = Operator switch
			{
				IKokoroApi.IConditionalActionApi.EquationOperator.Equal => ModEntry.Instance.Content.EqualSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => ModEntry.Instance.Content.NotEqualSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => ModEntry.Instance.Content.GreaterThanSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => ModEntry.Instance.Content.LessThanSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => ModEntry.Instance.Content.GreaterThanOrEqualSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => ModEntry.Instance.Content.LessThanOrEqualSprite,
				_ => throw new ArgumentException()
			};
			if (!dontRender)
				Draw.Sprite((Spr)operatorIcon.Id!.Value, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += SpriteLoader.Get((Spr)operatorIcon.Id!.Value)?.Width ?? 0;
		}

		Rhs.Render(g, ref position, isDisabled, dontRender);
	}
	
	private string GetTooltipDescriptionFormat()
		=> ModEntry.Instance.Localizations.Localize(["conditional", "equation", Style.ToString(), this.Operator.ToString()]);

	public string GetTooltipDescription(State state, Combat? combat)
		=> string.Format(GetTooltipDescriptionFormat(), Lhs.GetTooltipDescription(state, combat), Rhs.GetTooltipDescription(state, combat));

	public List<Tooltip> GetTooltips(State state, Combat? combat)
		=> [
			.. Lhs.GetTooltips(state, combat),
			.. Rhs.GetTooltips(state, combat),
		];
}