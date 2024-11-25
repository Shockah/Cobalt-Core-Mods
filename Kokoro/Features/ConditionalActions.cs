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
	#region V1
	
	public IKokoroApi.IConditionalActionApi ConditionalActions { get; } = new ConditionalActionApiImplementation();

	public sealed class ConditionalActionApiImplementation : IKokoroApi.IConditionalActionApi
	{
		public CardAction Make(IKokoroApi.IConditionalActionApi.IBoolExpression expression, CardAction action, bool fadeUnsatisfied = true)
			=> new AConditional { Expression = V1ToV2BoolExpressionWrapper.Convert(expression), Action = action, FadeUnsatisfied = fadeUnsatisfied, ShowQuestionMark = expression.ShouldRenderQuestionMark(MG.inst.g.state ?? DB.fakeState, MG.inst.g.state?.route as Combat) };

		public IKokoroApi.IConditionalActionApi.IIntExpression Constant(int value)
			=> new ConditionalActionIntConstant { Value = value };

		public IKokoroApi.IConditionalActionApi.IIntExpression HandConstant(int value)
			=> new ConditionalActionHandConstant { CurrentValue = value };

		public IKokoroApi.IConditionalActionApi.IIntExpression XConstant(int value)
			=> new ConditionalActionXConstant { CurrentValue = value };

		public IKokoroApi.IConditionalActionApi.IIntExpression ScalarMultiplier(IKokoroApi.IConditionalActionApi.IIntExpression expression, int scalar)
			=> new ConditionalActionScalarMultiplier { Expression = V1ToV2IntExpressionWrapper.Convert(expression), Scalar = scalar };

		public IKokoroApi.IConditionalActionApi.IBoolExpression HasStatus(Status status, bool targetPlayer = true, bool countNegative = false)
			=> new ConditionalActionHasStatusExpression { Status = status, TargetPlayer = targetPlayer, CountNegative = countNegative };

		public IKokoroApi.IConditionalActionApi.IIntExpression Status(Status status, bool targetPlayer = true)
			=> new ConditionalActionStatusExpression { Status = status, TargetPlayer = targetPlayer };

		public IKokoroApi.IConditionalActionApi.IBoolExpression Equation(
			IKokoroApi.IConditionalActionApi.IIntExpression lhs,
			IKokoroApi.IConditionalActionApi.EquationOperator @operator,
			IKokoroApi.IConditionalActionApi.IIntExpression rhs,
			IKokoroApi.IConditionalActionApi.EquationStyle style,
			bool hideOperator = false
		)
			=> new ConditionalActionEquation
			{
				Lhs = V1ToV2IntExpressionWrapper.Convert(lhs),
				Operator = (IKokoroApi.IV2.IConditionalApi.EquationOperator)(int)@operator,
				Rhs = V1ToV2IntExpressionWrapper.Convert(rhs),
				Style = (IKokoroApi.IV2.IConditionalApi.EquationStyle)(int)style,
				ShowOperator = !hideOperator,
			};
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IConditionalApi Conditional { get; } = new ConditionalApi();
		
		public sealed class ConditionalApi : IKokoroApi.IV2.IConditionalApi
		{
			public IKokoroApi.IV2.IConditionalApi.IConstantIntExpression? AsConstant(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IConstantIntExpression;

			public IKokoroApi.IV2.IConditionalApi.IConstantIntExpression Constant(int value)
				=> new ConditionalActionIntConstant { Value = value };

			public IKokoroApi.IV2.IConditionalApi.IHandConstantIntExpression? AsHandConstant(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IHandConstantIntExpression;

			public IKokoroApi.IV2.IConditionalApi.IHandConstantIntExpression HandConstant(int currentValue)
				=> new ConditionalActionHandConstant { CurrentValue = currentValue };

			public IKokoroApi.IV2.IConditionalApi.IXConstantIntExpression? AsXConstant(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IXConstantIntExpression;

			public IKokoroApi.IV2.IConditionalApi.IXConstantIntExpression XConstant(int currentValue)
				=> new ConditionalActionXConstant { CurrentValue = currentValue };

			public IKokoroApi.IV2.IConditionalApi.IMultiplyIntExpression? AsMultiply(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IMultiplyIntExpression;

			public IKokoroApi.IV2.IConditionalApi.IMultiplyIntExpression Multiply(IKokoroApi.IV2.IConditionalApi.IIntExpression expression, int scalar)
				=> new ConditionalActionScalarMultiplier { Expression = expression, Scalar = scalar };

			public IKokoroApi.IV2.IConditionalApi.IHasStatusExpression? AsHasStatus(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IHasStatusExpression;

			public IKokoroApi.IV2.IConditionalApi.IHasStatusExpression HasStatus(Status status, bool targetPlayer = true)
				=> new ConditionalActionHasStatusExpression { Status = status, TargetPlayer = targetPlayer };

			public IKokoroApi.IV2.IConditionalApi.IStatusExpression? AsStatus(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IStatusExpression;

			public IKokoroApi.IV2.IConditionalApi.IStatusExpression Status(Status status, bool targetPlayer = true)
				=> new ConditionalActionStatusExpression { Status = status, TargetPlayer = targetPlayer };

			public IKokoroApi.IV2.IConditionalApi.IEquation? AsEquation(IKokoroApi.IV2.IConditionalApi.IExpression expression)
				=> expression as IKokoroApi.IV2.IConditionalApi.IEquation;

			public IKokoroApi.IV2.IConditionalApi.IEquation Equation(IKokoroApi.IV2.IConditionalApi.IIntExpression lhs, IKokoroApi.IV2.IConditionalApi.EquationOperator @operator, IKokoroApi.IV2.IConditionalApi.IIntExpression rhs, IKokoroApi.IV2.IConditionalApi.EquationStyle style)
				=> new ConditionalActionEquation { Lhs = lhs, Operator = @operator, Rhs = rhs, Style = style };

			public IKokoroApi.IV2.IConditionalApi.IConditionalAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IConditionalApi.IConditionalAction;

			public IKokoroApi.IV2.IConditionalApi.IConditionalAction MakeAction(IKokoroApi.IV2.IConditionalApi.IBoolExpression expression, CardAction action)
				=> new AConditional { Expression = expression, Action = action };
		}
	}
}

internal sealed class ConditionalActionManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly ConditionalActionManager Instance = new();
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}
	
	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
	{
		if (args.Action is not AConditional conditional)
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
		if (conditional.ShowQuestionMark)
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

public sealed class AConditional : CardAction, IKokoroApi.IV2.IConditionalApi.IConditionalAction
{
	public required IKokoroApi.IV2.IConditionalApi.IBoolExpression Expression { get; set; }
	public required CardAction Action { get; set; }
	public bool FadeUnsatisfied { get; set; } = true;
	public bool ShowQuestionMark { get; set; } = true;

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (!Expression.GetValue(s, c))
			return;

		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		var combat = s.route as Combat ?? DB.fakeCombat;
		var description = Expression.GetTooltipDescription(s, combat);
		var formattedDescription = string.Format(ModEntry.Instance.Localizations.Localize(["conditional", "description"]), description);
		var defaultTooltip = new GlossaryTooltip($"AConditional::{formattedDescription}")
		{
			Icon = (Spr)ModEntry.Instance.Content.QuestionMarkSprite.Id!.Value,
			TitleColor = Colors.action,
			Title = ModEntry.Instance.Localizations.Localize(["conditional", "name"]),
			Description = formattedDescription
		};

		List<Tooltip> tooltips = [];
		tooltips.AddRange(Expression.OverrideConditionalTooltip(s, combat, defaultTooltip, formattedDescription));
		tooltips.AddRange(Expression.GetTooltips(s, combat));
		
		if (!Action.omitFromTooltips)
			tooltips.AddRange(Action.GetTooltips(s));
		return tooltips;
	}
	
	public IKokoroApi.IV2.IConditionalApi.IConditionalAction SetExpression(IKokoroApi.IV2.IConditionalApi.IBoolExpression value)
	{
		Expression = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IConditionalAction SetAction(CardAction value)
	{
		Action = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IConditionalAction SetFadeUnsatisfied(bool value)
	{
		FadeUnsatisfied = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IConditionalAction SetShowQuestionMark(bool value)
	{
		ShowQuestionMark = value;
		return this;
	}
}

internal sealed class ConditionalActionIntConstant : IKokoroApi.IV2.IConditionalApi.IConstantIntExpression, IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public required int Value { get; set; }

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			BigNumbers.Render(Value, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Value}".Length * 6;
	}

	public string GetTooltipDescription(State state, Combat combat)
		=> $"<c=boldPink>{Value}</c>";

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);
	
	public IKokoroApi.IV2.IConditionalApi.IConstantIntExpression SetValue(int value)
	{
		Value = value;
		return this;
	}
}

internal sealed class ConditionalActionHandConstant : IKokoroApi.IV2.IConditionalApi.IHandConstantIntExpression, IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public required int CurrentValue { get; set; }

	public int GetValue(State state, Combat combat)
		=> CurrentValue;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(StableSpr.icons_dest_hand, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public string GetTooltipDescription(State state, Combat combat)
		=> ModEntry.Instance.Localizations.Localize(["conditional", "hand"]);

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);
	
	public IKokoroApi.IV2.IConditionalApi.IHandConstantIntExpression SetCurrentValue(int value)
	{
		CurrentValue = value;
		return this;
	}
}

internal sealed class ConditionalActionXConstant : IKokoroApi.IV2.IConditionalApi.IXConstantIntExpression, IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public required int CurrentValue { get; set; }

	public int GetValue(State state, Combat combat)
		=> CurrentValue;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(StableSpr.icons_x, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public string GetTooltipDescription(State state, Combat combat)
		=> ModEntry.Instance.Localizations.Localize(["conditional", "x"]);

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);
	
	public IKokoroApi.IV2.IConditionalApi.IXConstantIntExpression SetCurrentValue(int value)
	{
		CurrentValue = value;
		return this;
	}
}

internal sealed class ConditionalActionScalarMultiplier : IKokoroApi.IV2.IConditionalApi.IMultiplyIntExpression, IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public required IKokoroApi.IV2.IConditionalApi.IIntExpression Expression { get; set; }

	[JsonProperty]
	public required int Scalar { get; set; }

	[JsonIgnore]
	internal int? ConstantValue
		=> this.Expression switch
		{
			ConditionalActionIntConstant constant => constant.Value,
			ConditionalActionScalarMultiplier scalarMultiplier => scalarMultiplier.ConstantValue * this.Scalar,
			_ => null
		};

	public int GetValue(State state, Combat combat)
		=> Expression.GetValue(state, combat) * Scalar;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		var constantValue = ConstantValue;
		if (constantValue is not null)
		{
			new ConditionalActionIntConstant { Value = constantValue.Value }.Render(g, ref position, isDisabled, dontRender);
			return;
		}

		if (!dontRender)
			BigNumbers.Render(Scalar, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Scalar}".Length * 6;
		Expression.Render(g, ref position, isDisabled, dontRender);
	}

	public string GetTooltipDescription(State state, Combat combat)
	{
		var constantValue = ConstantValue;
		if (constantValue is not null)
			return new ConditionalActionIntConstant { Value = constantValue.Value }.GetTooltipDescription(state, combat);
		return $"<c=boldPink>{Scalar}</c>x {Expression.GetTooltipDescription(state, combat)}";
	}

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);
	
	public IKokoroApi.IV2.IConditionalApi.IMultiplyIntExpression SetExpression(IKokoroApi.IV2.IConditionalApi.IIntExpression value)
	{
		Expression = value;
		return this;
	}
	
	public IKokoroApi.IV2.IConditionalApi.IMultiplyIntExpression SetScalar(int value)
	{
		Scalar = value;
		return this;
	}
}

internal sealed class ConditionalActionHasStatusExpression : IKokoroApi.IV2.IConditionalApi.IHasStatusExpression, IKokoroApi.IConditionalActionApi.IBoolExpression
{
	[JsonProperty]
	public required Status Status { get; set; }

	[JsonProperty]
	public required bool TargetPlayer { get; set; }

	[JsonProperty]
	public bool CountNegative { get; set; }

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

	public string GetTooltipDescription(State state, Combat combat)
		=> string.Format(ModEntry.Instance.Localizations.Localize(["conditional", "hasStatus", this.TargetPlayer ? "player" : "enemy"]), $"<c=status>{Status.GetLocName().ToUpper()}</c>");

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);

	public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat?.otherShip;
		var amount = ship?.Get(Status) ?? 1;
		return [new TTGlossary($"status.{Status.Key()}", amount)];
	}

	List<Tooltip> IKokoroApi.IConditionalActionApi.IExpression.GetTooltips(State state, Combat? combat)
		=> GetTooltips(state, combat ?? DB.fakeCombat).ToList();
	
	public IKokoroApi.IV2.IConditionalApi.IHasStatusExpression SetStatus(Status value)
	{
		Status = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IHasStatusExpression SetTargetPlayer(bool value)
	{
		TargetPlayer = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IHasStatusExpression SetCountNegative(bool value)
	{
		CountNegative = value;
		return this;
	}
}

internal sealed class ConditionalActionStatusExpression : IKokoroApi.IV2.IConditionalApi.IStatusExpression, IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public required Status Status { get; set; }

	[JsonProperty]
	public required bool TargetPlayer { get; set; }

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

	public string GetTooltipDescription(State state, Combat combat)
		=> $"<c=status>{Status.GetLocName().ToUpper()}</c>";

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);

	public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat?.otherShip;
		var amount = ship?.Get(Status) ?? 1;
		return [new TTGlossary($"status.{this.Status.Key()}", amount)];
	}

	List<Tooltip> IKokoroApi.IConditionalActionApi.IExpression.GetTooltips(State state, Combat? combat)
		=> GetTooltips(state, combat ?? DB.fakeCombat).ToList();
	
	public IKokoroApi.IV2.IConditionalApi.IStatusExpression SetStatus(Status value)
	{
		Status = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IStatusExpression SetTargetPlayer(bool value)
	{
		TargetPlayer = value;
		return this;
	}
}

internal sealed class ConditionalActionEquation : IKokoroApi.IV2.IConditionalApi.IEquation, IKokoroApi.IConditionalActionApi.IBoolExpression
{
	[JsonProperty]
	public required IKokoroApi.IV2.IConditionalApi.IIntExpression Lhs { get; set; }

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public required IKokoroApi.IV2.IConditionalApi.EquationOperator Operator { get; set; }

	[JsonProperty]
	public required IKokoroApi.IV2.IConditionalApi.IIntExpression Rhs { get; set; }

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public required IKokoroApi.IV2.IConditionalApi.EquationStyle Style { get; set; }

	[JsonProperty]
	public bool ShowOperator { get; set; } = true;

	public bool GetValue(State state, Combat combat)
	{
		var lhs = Lhs.GetValue(state, combat);
		var rhs = Rhs.GetValue(state, combat);
		return Operator switch
		{
			IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal => lhs == rhs,
			IKokoroApi.IV2.IConditionalApi.EquationOperator.NotEqual => lhs != rhs,
			IKokoroApi.IV2.IConditionalApi.EquationOperator.GreaterThan => lhs > rhs,
			IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThan => lhs < rhs,
			IKokoroApi.IV2.IConditionalApi.EquationOperator.GreaterThanOrEqual => lhs >= rhs,
			IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThanOrEqual => lhs <= rhs,
			_ => throw new ArgumentException()
		};
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		Lhs.Render(g, ref position, isDisabled, dontRender);

		if (!ShowOperator)
		{
			var operatorIcon = Operator switch
			{
				IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal => ModEntry.Instance.Content.EqualSprite,
				IKokoroApi.IV2.IConditionalApi.EquationOperator.NotEqual => ModEntry.Instance.Content.NotEqualSprite,
				IKokoroApi.IV2.IConditionalApi.EquationOperator.GreaterThan => ModEntry.Instance.Content.GreaterThanSprite,
				IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThan => ModEntry.Instance.Content.LessThanSprite,
				IKokoroApi.IV2.IConditionalApi.EquationOperator.GreaterThanOrEqual => ModEntry.Instance.Content.GreaterThanOrEqualSprite,
				IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThanOrEqual => ModEntry.Instance.Content.LessThanOrEqualSprite,
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

	public string GetTooltipDescription(State state, Combat combat)
		=> string.Format(GetTooltipDescriptionFormat(), Lhs.GetTooltipDescription(state, combat), Rhs.GetTooltipDescription(state, combat));

	string IKokoroApi.IConditionalActionApi.IExpression.GetTooltipDescription(State state, Combat? combat)
		=> GetTooltipDescription(state, combat ?? DB.fakeCombat);

	public List<Tooltip> GetTooltips(State state, Combat combat)
		=> [
			.. Lhs.GetTooltips(state, combat),
			.. Rhs.GetTooltips(state, combat),
		];

	List<Tooltip> IKokoroApi.IConditionalActionApi.IExpression.GetTooltips(State state, Combat? combat)
		=> GetTooltips(state, combat ?? DB.fakeCombat);
	
	public IKokoroApi.IV2.IConditionalApi.IEquation SetLhs(IKokoroApi.IV2.IConditionalApi.IIntExpression value)
	{
		Lhs = value;
		return this;
	}
	
	public IKokoroApi.IV2.IConditionalApi.IEquation SetOperator(IKokoroApi.IV2.IConditionalApi.EquationOperator value)
	{
		Operator = value;
		return this;
	}
	
	public IKokoroApi.IV2.IConditionalApi.IEquation SetRhs(IKokoroApi.IV2.IConditionalApi.IIntExpression value)
	{
		Rhs = value;
		return this;
	}
	
	public IKokoroApi.IV2.IConditionalApi.IEquation SetStyle(IKokoroApi.IV2.IConditionalApi.EquationStyle value)
	{
		Style = value;
		return this;
	}

	public IKokoroApi.IV2.IConditionalApi.IEquation SetShowOperator(bool value)
	{
		ShowOperator = value;
		return this;
	}
}

internal sealed class V1ToV2BoolExpressionWrapper(IKokoroApi.IConditionalActionApi.IBoolExpression v1) : IKokoroApi.IV2.IConditionalApi.IBoolExpression
{
	internal static IKokoroApi.IV2.IConditionalApi.IBoolExpression Convert(IKokoroApi.IConditionalActionApi.IBoolExpression v1)
		=> v1 switch
		{
			IKokoroApi.IV2.IConditionalApi.IBoolExpression v2 => v2,
			_ => new V1ToV2BoolExpressionWrapper(v1)
		};
	
	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
		=> v1.Render(g, ref position, isDisabled, dontRender);

	public string GetTooltipDescription(State state, Combat combat)
		=> v1.GetTooltipDescription(state, combat);
	
	public List<Tooltip> GetTooltips(State state, Combat combat)
		=> v1.GetTooltips(state, combat);

	public bool GetValue(State state, Combat combat)
		=> v1.GetValue(state, combat);

	public IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat combat, Tooltip defaultTooltip, string defaultTooltipDescription)
		=> v1.OverrideConditionalTooltip(state, combat, defaultTooltip, defaultTooltipDescription);
}

internal sealed class V1ToV2IntExpressionWrapper(IKokoroApi.IConditionalActionApi.IIntExpression v1) : IKokoroApi.IV2.IConditionalApi.IIntExpression
{
	internal static IKokoroApi.IV2.IConditionalApi.IIntExpression Convert(IKokoroApi.IConditionalActionApi.IIntExpression v1)
		=> v1 switch
		{
			IKokoroApi.IV2.IConditionalApi.IIntExpression v2 => v2,
			_ => new V1ToV2IntExpressionWrapper(v1)
		};
	
	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
		=> v1.Render(g, ref position, isDisabled, dontRender);

	public string GetTooltipDescription(State state, Combat combat)
		=> v1.GetTooltipDescription(state, combat);
	
	public List<Tooltip> GetTooltips(State state, Combat combat)
		=> v1.GetTooltips(state, combat);

	public int GetValue(State state, Combat combat)
		=> v1.GetValue(state, combat);
}