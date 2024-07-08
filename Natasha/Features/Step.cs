using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class Step : IRegisterable, IWrappedActionHook
{
	private static ISpriteEntry BaseIcon = null!;
	private static readonly Dictionary<(int, int), Spr> Icons = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BaseIcon = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/Step.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);

		var self = new Step();
		ModEntry.Instance.KokoroApi.Actions.RegisterWrappedActionHook(self, 0);
	}

	internal static Spr ObtainIcon(int step, int steps)
	{
		steps = Math.Max(steps, 1);
		step = (step - 1) % steps + 1;
		if (Icons.TryGetValue((step, steps), out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Step{step}of{steps}", () =>
		{
			var baseIcon = SpriteLoader.Get(BaseIcon.Sprite)!;
			return TextureUtils.CreateTexture(baseIcon.Width, baseIcon.Height, () =>
			{
				Draw.Sprite(baseIcon, 0, 0);

				Draw.Text(step.ToString(), 0, 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				var textRect = Draw.Text(steps.ToString(), 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
				Draw.Text(steps.ToString(), baseIcon.Width - textRect.w, baseIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
			});
		}).Sprite;

		Icons[(step, steps)] = icon;
		return icon;
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not StepAction stepAction)
			return true;

		var timesPlayed = state.FindCard(stepAction.CardId) is { } card ? card.GetTimesPlayed() + 1 : -1;
		var step = (timesPlayed - 1) % stepAction.Steps + 1;
		var selfDisabled = stepAction.disabled || (timesPlayed != -1 && step != stepAction.Step);
		var oldActionDisabled = stepAction.Action.disabled;
		stepAction.Action.disabled = selfDisabled;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ObtainIcon(stepAction.Step, stepAction.Steps), position.x, position.y, color: selfDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 14;

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, stepAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		stepAction.Action.disabled = oldActionDisabled;

		return false;
	}

	public List<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is StepAction stepAction ? [stepAction.Action] : null;
}

internal sealed class StepAction : CardAction
{
	public required int CardId;
	public required CardAction Action;
	public required int Step;
	public required int Steps;

	public override Icon? GetIcon(State s)
		=> new(Natasha.Step.ObtainIcon(Step, Steps), null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
	{
		int currentStep;
		if (s.route is Combat && s.FindCard(CardId) is { } card)
			currentStep = card.GetTimesPlayed() % Steps + 1;
		else
			currentStep = -1;

		return [
			new GlossaryTooltip($"action.{GetType().Namespace!}::Step{Step}of{Steps}")
				{
					Icon = Natasha.Step.ObtainIcon(Step, Steps),
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "Step", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(
						["action", "Step", "description", currentStep == -1 ? "stateless" : "stateful"],
						currentStep == -1
							? new { Step = Step, Steps = Steps }
							: new { Step = Step, Steps = Steps, Current = currentStep }
					),
				},
				.. Action.GetTooltips(s)
		];
	}

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (s.FindCard(CardId) is not { } card)
			return;

		var step = (card.GetTimesPlayed() - 1) % Steps + 1;
		if (step != Step)
			return;

		c.QueueImmediate(Action);
	}
}