using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;

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
			{
				if (sequenceLength > 8)
					throw new NotSupportedException("Only sequences up to 8-long are supported");
				if (sequenceStep < 1 || sequenceStep > sequenceLength)
					throw new ArgumentOutOfRangeException(nameof(sequenceStep));
				return new SequenceAction { CardId = cardId, Interval = interval, SequenceStep = sequenceStep, SequenceLength = sequenceLength, Action = action };
			}
		}
	}
}

internal sealed class SequenceManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	private readonly struct IconDescriptor(IKokoroApi.IV2.ISequenceApi.Interval interval, IconDescriptor.PipDescriptor[] pips)
		: IEquatable<IconDescriptor>
	{
		public readonly struct PipDescriptor(bool isOn, Color color) : IEquatable<PipDescriptor>
		{
			private readonly bool IsOn = isOn;
			private readonly Color Color = color;

			public override bool Equals(object? obj)
				=> obj is PipDescriptor other && Equals(other);

			public bool Equals(PipDescriptor other)
				=> IsOn == other.IsOn && Color.ToInt().Equals(other.Color.ToInt());

			public override int GetHashCode()
				=> HashCode.Combine(IsOn, Color.ToInt());

			public override string ToString()
				=> $"{(IsOn ? "On" : "Off")}{Color.ToString().ToUpperInvariant()}";
		}

		private readonly IKokoroApi.IV2.ISequenceApi.Interval Interval = interval;
		private readonly PipDescriptor[] Pips = pips;

		public override bool Equals(object? obj)
			=> obj is IconDescriptor other && Equals(other);

		public bool Equals(IconDescriptor other)
			=> Interval == other.Interval && Pips.SequenceEqual(other.Pips);

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(Interval);
			foreach (var pip in Pips)
				hashCode.Add(pip);
			return hashCode.ToHashCode();
		}

		public override string ToString()
			=> $"{Interval}::{string.Join("_", Pips)}";
	}
	
	internal static readonly SequenceManager Instance = new();

	private static ISpriteEntry Sheet = null!;
	private static readonly Dictionary<IconDescriptor, Spr> Icons = [];

	internal static void Setup(IHarmony harmony)
	{
		Sheet = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/SequenceSheet.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	internal static Spr ObtainIcon(IKokoroApi.IV2.ISequenceApi.Interval interval, int sequenceStep, int sequenceLength, int sequenceAt, Color? actionOnColor, Color? actionOffColor, Color? inactionOnColor, Color? inactionOffColor)
	{
		sequenceLength = Math.Max(sequenceLength, 1);
		sequenceStep = (sequenceStep - 1) % sequenceLength + 1;
		sequenceAt = (sequenceAt - 1) % sequenceLength + 1;
		
		if (sequenceLength > 8)
			throw new NotSupportedException("Only sequences up to 8-long are supported");
		
		var defaultedActionOnColor = actionOnColor ?? new Color("fc72dc");
		var defaultedInactionOnColor = inactionOnColor ?? Colors.disabledIconTint;
		var defaultedActionOffColor = actionOffColor ?? defaultedActionOnColor.gain(0.4);
		var defaultedInactionOffColor = inactionOffColor ?? defaultedInactionOnColor.gain(0.4);
		
		var iconDescriptor = new IconDescriptor(
			interval,
			Enumerable.Range(1, sequenceLength)
				.Select(i => new IconDescriptor.PipDescriptor(sequenceAt == i, GetPipColor(i)))
				.ToArray()
		);

		if (Icons.TryGetValue(iconDescriptor, out var icon))
			return icon;
		
		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Sequence::{iconDescriptor}", () =>
		{
			return TextureUtils.CreateTexture(new(9, 9)
			{
				Actions = () =>
				{
					Draw.Sprite(Sheet.Sprite, 0, 0, pixelRect: GetTypeRegion());
					Draw.Sprite(Sheet.Sprite, 0, 0, pixelRect: GetBaseRegion());

					for (var i = 1; i <= sequenceLength; i++)
						Draw.Sprite(Sheet.Sprite, 0, 0, pixelRect: GetPipRegion(i, i == sequenceAt), color: GetPipColor(i));
				},
			});
		}).Sprite;

		Icons[iconDescriptor] = icon;
		return icon;

		Color GetPipColor(int step)
			=> step == sequenceStep
				? (step == sequenceAt ? defaultedActionOnColor : defaultedActionOffColor)
				: (step == sequenceAt ? defaultedInactionOnColor : defaultedInactionOffColor);

		Rect GetTypeRegion()
			=> new((int)interval * 9, 0, 9, 9);

		Rect GetBaseRegion()
			=> new(0, (sequenceLength - 1) * 9, 9, 9);

		Rect GetPipRegion(int step, bool isOn)
			=> new(9 + (step - 1) * 18 + (isOn ? 9 : 0), (sequenceLength - 1) * 9, 9, 9);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not SequenceAction sequenceAction)
			return true;

		int? nextTimesPlayed = state.FindCard(sequenceAction.CardId) is { } card ? ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)sequenceAction.Interval) + 1 : null;
		var step = ((nextTimesPlayed ?? 1) - 1) % sequenceAction.SequenceLength + 1;
		var selfDisabled = sequenceAction.disabled || (nextTimesPlayed is not null && step != sequenceAction.SequenceStep);
		var oldActionDisabled = sequenceAction.Action.disabled;
		sequenceAction.Action.disabled = selfDisabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(
				ObtainIcon(
					sequenceAction.Interval, sequenceAction.SequenceStep, sequenceAction.SequenceLength, step,
					sequenceAction.ActionOnColor, sequenceAction.ActionOffColor, sequenceAction.InactionOnColor, sequenceAction.InactionOffColor
				),
				position.x, position.y, color: sequenceAction.disabled ? Colors.disabledIconTint : Colors.white
			);
		position.x += 10;

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
	public Color? ActionOnColor { get; set; }
	public Color? ActionOffColor { get; set; }
	public Color? InactionOnColor { get; set; }
	public Color? InactionOffColor { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override Icon? GetIcon(State s)
		=> new(SequenceManager.ObtainIcon(Interval, SequenceStep, SequenceLength, 1, ActionOnColor, ActionOffColor, InactionOnColor, InactionOffColor), null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
	{
		int? nextTimesPlayed = s.FindCard(CardId) is { } card ? ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)Interval) + 1 : null;
		var step = ((nextTimesPlayed ?? 1) - 1) % SequenceLength + 1;

		string description;
		if (nextTimesPlayed is null)
			description = ModEntry.Instance.Localizations.Localize(["sequence", Interval.ToString(), "description", "stateless"], new { Step = SequenceStep, Steps = SequenceLength });
		else if (step == SequenceStep)
			description = ModEntry.Instance.Localizations.Localize(["sequence", Interval.ToString(), "description", "stateful", "now"], new { Step = SequenceStep, Steps = SequenceLength });
		else
			description = ModEntry.Instance.Localizations.Localize(["sequence", Interval.ToString(), "description", "stateful", "other"], new { Step = SequenceStep, Steps = SequenceLength, Plays = SequenceLength - step });

		return [
			new GlossaryTooltip($"action.{GetType().Namespace!}::SequenceThis{Interval}{SequenceLength}")
			{
				Icon = SequenceManager.ObtainIcon(Interval, SequenceStep, SequenceLength, step, ActionOnColor, ActionOffColor, InactionOnColor, InactionOffColor),
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["sequence", Interval.ToString(), "name"]),
				Description = description,
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

		Action.whoDidThis = whoDidThis;
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

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetActionOnColor(Color? value)
	{
		ActionOnColor = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetActionOffColor(Color? value)
	{
		ActionOffColor = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetInactionOnColor(Color? value)
	{
		InactionOnColor = value;
		return this;
	}

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetInactionOffColor(Color? value)
	{
		InactionOffColor = value;
		return this;
	}
}