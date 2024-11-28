using daisyowl.text;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ISequenceApi Sequence { get; } = new SequenceApi();
		
		public sealed class SequenceApi : IKokoroApi.IV2.ISequenceApi
		{
			public IKokoroApi.IV2.ISequenceApi.ISequenceAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.ISequenceApi.ISequenceAction;

			public IKokoroApi.IV2.ISequenceApi.ISequenceAction MakeAction(int cardId, IKokoroApi.IV2.ISequenceApi.Interval interval, int sequenceStep, int sequenceLength, CardAction action)
				=> new SequenceAction { CardId = cardId, Interval = interval, SequenceStep = sequenceStep, SequenceLength = sequenceLength, Action = action };
		}
	}
}

internal sealed class SequenceManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly SequenceManager Instance = new();

	private static ISpriteEntry BaseIconRun = null!;
	private static ISpriteEntry BaseIconCombat = null!;
	private static ISpriteEntry BaseIconTurn = null!;
	private static readonly Dictionary<(int, int), Spr> IconsRun = [];
	private static readonly Dictionary<(int, int), Spr> IconsCombat = [];
	private static readonly Dictionary<(int, int), Spr> IconsTurn = [];

	internal static void Setup(IHarmony harmony)
	{
		BaseIconRun = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/SequenceThisRun.png"));
		BaseIconCombat = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/SequenceThisCombat.png"));
		BaseIconTurn = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/SequenceThisTurn.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	internal static Spr ObtainIcon(IKokoroApi.IV2.ISequenceApi.Interval interval, int sequenceStep, int sequenceLength)
	{
		var icons = GetIcons();
		
		sequenceLength = Math.Max(sequenceLength, 1);
		sequenceStep = (sequenceStep - 1) % sequenceLength + 1;
		if (icons.TryGetValue((sequenceStep, sequenceLength), out var icon))
			return icon;

		
		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"SequenceThis{interval}{sequenceStep}of{sequenceLength}", () =>
		{
			var baseIcon = SpriteLoader.Get(GetBaseIcon().Sprite)!;
			return TextureUtils.CreateTexture(baseIcon.Width, baseIcon.Height + 2, () =>
			{
				Draw.Sprite(baseIcon, 0, 1);
				Draw.Text(sequenceStep.ToString(), 6, 1, color: Colors.white, align: TAlign.Right, outline: Colors.black, dontSubstituteLocFont: true);
				Draw.Text(sequenceLength.ToString(), 9, 4, color: Colors.white, align: TAlign.Left, outline: Colors.black, dontSubstituteLocFont: true);
			});
		}).Sprite;

		icons[(sequenceStep, sequenceLength)] = icon;
		return icon;
		
		ISpriteEntry GetBaseIcon()
			=> interval switch
			{
				IKokoroApi.IV2.ISequenceApi.Interval.Run => BaseIconRun,
				IKokoroApi.IV2.ISequenceApi.Interval.Combat => BaseIconCombat,
				IKokoroApi.IV2.ISequenceApi.Interval.Turn => BaseIconTurn,
				_ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
			};
		
		Dictionary<(int, int), Spr> GetIcons()
			=> interval switch
			{
				IKokoroApi.IV2.ISequenceApi.Interval.Run => IconsRun,
				IKokoroApi.IV2.ISequenceApi.Interval.Combat => IconsCombat,
				IKokoroApi.IV2.ISequenceApi.Interval.Turn => IconsTurn,
				_ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
			};
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not SequenceAction sequenceAction)
			return true;

		var timesPlayed = state.FindCard(sequenceAction.CardId) is { } card ? ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)sequenceAction.Interval) + 1 : -1;
		var step = (timesPlayed - 1) % sequenceAction.SequenceLength + 1;
		var selfDisabled = sequenceAction.disabled || (timesPlayed != -1 && step != sequenceAction.SequenceStep);
		var oldActionDisabled = sequenceAction.Action.disabled;
		sequenceAction.Action.disabled = selfDisabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ObtainIcon(sequenceAction.Interval, sequenceAction.SequenceStep, sequenceAction.SequenceLength), position.x, position.y - 1, color: selfDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 15;

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, sequenceAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		sequenceAction.Action.disabled = oldActionDisabled;

		return false;
	}

	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
		=> args.Action is SequenceAction sequenceAction ? [sequenceAction.Action] : null;
}

internal sealed class SequenceAction : CardAction, IKokoroApi.IV2.ISequenceApi.ISequenceAction
{
	public required int CardId { get; set; }
	public required IKokoroApi.IV2.ISequenceApi.Interval Interval { get; set; }
	public required CardAction Action { get; set; }
	public required int SequenceStep { get; set; }
	public required int SequenceLength { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override Icon? GetIcon(State s)
		=> new(SequenceManager.ObtainIcon(Interval, SequenceStep, SequenceLength), null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
	{
		int currentSequenceStep;
		if (s.route is Combat && s.FindCard(CardId) is { } card)
			currentSequenceStep = ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)Interval) % SequenceLength + 1;
		else
			currentSequenceStep = -1;

		return [
			new GlossaryTooltip($"action.{GetType().Namespace!}::SequenceThis{Interval}{SequenceLength}")
			{
				Icon = SequenceManager.ObtainIcon(Interval, SequenceStep, SequenceLength),
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["sequence", Interval.ToString(), "name"]),
				Description = ModEntry.Instance.Localizations.Localize(
					["sequence", Interval.ToString(), "description", currentSequenceStep == -1 ? "stateless" : "stateful"],
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

		var sequenceStep = (ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)Interval) - 1) % SequenceLength + 1;
		if (sequenceStep != SequenceStep)
			return;

		c.QueueImmediate(Action);
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetCardId(int value)
	{
		CardId = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetInterval(IKokoroApi.IV2.ISequenceApi.Interval value)
	{
		Interval = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetSequenceStep(int value)
	{
		SequenceStep = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetSequenceLength(int value)
	{
		SequenceLength = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetAction(CardAction value)
	{
		Action = value;
		return this;
	}
}