using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class Sequence : IRegisterable, IWrappedActionHook
{
	private static ISpriteEntry BaseIcon = null!;
	private static readonly Dictionary<(int, int), Spr> Icons = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BaseIcon = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/Sequence.png"));

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);

		var self = new Sequence();
		ModEntry.Instance.KokoroApi.Actions.RegisterWrappedActionHook(self, 0);
	}

	internal static Spr ObtainIcon(int sequenceStep, int sequenceLength)
	{
		sequenceLength = Math.Max(sequenceLength, 1);
		sequenceStep = (sequenceStep - 1) % sequenceLength + 1;
		if (Icons.TryGetValue((sequenceStep, sequenceLength), out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Sequence{sequenceStep}of{sequenceLength}", () =>
		{
			var baseIcon = SpriteLoader.Get(BaseIcon.Sprite)!;
			return TextureUtils.CreateTexture(baseIcon.Width, baseIcon.Height, () =>
			{
				Draw.Sprite(baseIcon, 0, 0);

				Draw.Text(sequenceStep.ToString(), 1, 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				var textRect = Draw.Text(sequenceLength.ToString(), 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
				Draw.Text(sequenceLength.ToString(), baseIcon.Width - textRect.w, baseIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
			});
		}).Sprite;

		Icons[(sequenceStep, sequenceLength)] = icon;
		return icon;
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not SequenceAction sequenceAction)
			return true;

		var timesPlayed = state.FindCard(sequenceAction.CardId) is { } card ? ModEntry.Instance.KokoroApi.Actions.GetTimesPlayed(card) + 1 : -1;
		var step = (timesPlayed - 1) % sequenceAction.SequenceLength + 1;
		var selfDisabled = sequenceAction.disabled || (timesPlayed != -1 && step != sequenceAction.SequenceStep);
		var oldActionDisabled = sequenceAction.Action.disabled;
		sequenceAction.Action.disabled = selfDisabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ObtainIcon(sequenceAction.SequenceStep, sequenceAction.SequenceLength), position.x, position.y, color: selfDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 14;

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, sequenceAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		sequenceAction.Action.disabled = oldActionDisabled;

		return false;
	}

	public List<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is SequenceAction sequenceAction ? [sequenceAction.Action] : null;
}

internal sealed class SequenceAction : CardAction
{
	public required int CardId;
	public required CardAction Action;
	public required int SequenceStep;
	public required int SequenceLength;

	public override Icon? GetIcon(State s)
		=> new(Sequence.ObtainIcon(SequenceStep, SequenceLength), null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
	{
		int currentSequenceStep;
		if (s.route is Combat && s.FindCard(CardId) is { } card)
			currentSequenceStep = ModEntry.Instance.KokoroApi.Actions.GetTimesPlayed(card) % SequenceLength + 1;
		else
			currentSequenceStep = -1;

		return [
			new GlossaryTooltip($"action.{GetType().Namespace!}::Sequence{SequenceLength}")
				{
					Icon = Sequence.ObtainIcon(SequenceStep, SequenceLength),
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "Sequence", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(
						["action", "Sequence", "description", currentSequenceStep == -1 ? "stateless" : "stateful"],
						currentSequenceStep == -1
							? new { Step = SequenceStep, Steps = SequenceLength }
							: new { Step = SequenceStep, Steps = SequenceLength, Current = currentSequenceStep }
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

		var sequenceStep = (ModEntry.Instance.KokoroApi.Actions.GetTimesPlayed(card) - 1) % SequenceLength + 1;
		if (sequenceStep != SequenceStep)
			return;

		c.QueueImmediate(Action);
	}
}