using System;
using System.Collections.Generic;
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
				if (sequenceLength > 5)
					throw new NotSupportedException("Only sequences up to 5-long are supported");
				if (sequenceStep < 1 || sequenceStep > sequenceLength)
					throw new ArgumentOutOfRangeException(nameof(sequenceStep));
				return new SequenceAction { CardId = cardId, Interval = interval, SequenceStep = sequenceStep, SequenceLength = sequenceLength, Action = action };
			}
		}
	}
}

internal sealed class SequenceManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly SequenceManager Instance = new();

	private static ISpriteEntry BaseRunIcon = null!;
	private static ISpriteEntry BaseCombatIcon = null!;
	private static ISpriteEntry BaseTurnIcon = null!;
	private static readonly Dictionary<(string, int), Spr> CycleBorderIcons = [];
	private static readonly Dictionary<(int, int), Spr> CycleIcons = [];
	private static readonly Dictionary<(string, int, int), Spr> RunIcons = [];
	private static readonly Dictionary<(string, int, int), Spr> CombatIcons = [];
	private static readonly Dictionary<(string, int, int), Spr> TurnIcons = [];

	internal static void Setup(IHarmony harmony)
	{
		BaseRunIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Sequence/TypeRun.png"));
		BaseCombatIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Sequence/TypeCombat.png"));
		BaseTurnIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Sequence/TypeTurn.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	internal static Spr ObtainIcon(IKokoroApi.IV2.ISequenceApi.Interval interval, int sequenceStep, int sequenceLength, Color? tint)
	{
		var icons = GetIcons();
		var defaultedTint = tint ?? new Color("fc72dc");
		
		sequenceLength = Math.Max(sequenceLength, 1);
		sequenceStep = (sequenceStep - 1) % sequenceLength + 1;
		if (icons.TryGetValue((defaultedTint.ToString(), sequenceStep, sequenceLength), out var icon))
			return icon;
		
		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"SequenceThis{interval}{sequenceStep}of{sequenceLength}", () =>
		{
			var baseIcon = SpriteLoader.Get(GetBaseIcon().Sprite)!;
			return TextureUtils.CreateTexture(baseIcon.Width, baseIcon.Height, () =>
			{
				Draw.Sprite(baseIcon, 0, 0);
				Draw.Sprite(GetCycleBorderIcon(sequenceLength), 0, 0, color: defaultedTint.gain(0.75));

				for (var i = 1; i <= sequenceLength; i++)
				{
					var pipColor = defaultedTint.gain(i == sequenceStep ? 1 : 0.4);
					Draw.Sprite(GetCycleIcon(i, sequenceLength), 0, 0, color: pipColor);
				}
			});
		}).Sprite;

		icons[(defaultedTint.ToString(), sequenceStep, sequenceLength)] = icon;
		return icon;
		
		ISpriteEntry GetBaseIcon()
			=> interval switch
			{
				IKokoroApi.IV2.ISequenceApi.Interval.Run => BaseRunIcon,
				IKokoroApi.IV2.ISequenceApi.Interval.Combat => BaseCombatIcon,
				IKokoroApi.IV2.ISequenceApi.Interval.Turn => BaseTurnIcon,
				_ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
			};

		Spr GetCycleBorderIcon(int length)
		{
			if (!CycleBorderIcons.TryGetValue((tint.ToString()!, length), out var cycleBorderIcon))
			{
				cycleBorderIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Sequence/CycleBorder{length}.png")).Sprite;
				CycleBorderIcons[(tint.ToString()!, length)] = cycleBorderIcon;
			}
			return cycleBorderIcon;
		}

		Spr GetCycleIcon(int step, int length)
		{
			if (!CycleIcons.TryGetValue((step, length), out var cycleIcon))
			{
				cycleIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Sequence/Cycle{step}of{length}.png")).Sprite;
				CycleIcons[(step, length)] = cycleIcon;
			}
			return cycleIcon;
		}
		
		Dictionary<(string, int, int), Spr> GetIcons()
			=> interval switch
			{
				IKokoroApi.IV2.ISequenceApi.Interval.Run => RunIcons,
				IKokoroApi.IV2.ISequenceApi.Interval.Combat => CombatIcons,
				IKokoroApi.IV2.ISequenceApi.Interval.Turn => TurnIcons,
				_ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
			};
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not SequenceAction sequenceAction)
			return true;

		int? nextTimesPlayed = state.FindCard(sequenceAction.CardId) is { } card ? ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)sequenceAction.Interval) + 1 : null;
		var step = ((nextTimesPlayed ?? 1) - 1) % sequenceAction.SequenceLength + 1;
		var iconStep = (sequenceAction.SequenceStep - step + sequenceAction.SequenceLength) % sequenceAction.SequenceLength + 1;
		var selfDisabled = sequenceAction.disabled || (nextTimesPlayed is not null && step != sequenceAction.SequenceStep);
		var oldActionDisabled = sequenceAction.Action.disabled;
		sequenceAction.Action.disabled = selfDisabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ObtainIcon(sequenceAction.Interval, iconStep, sequenceAction.SequenceLength, sequenceAction.Tint), position.x, position.y, color: sequenceAction.disabled ? Colors.disabledIconTint : Colors.white);
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
	public Color? Tint { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override Icon? GetIcon(State s)
		=> new(SequenceManager.ObtainIcon(Interval, SequenceStep, SequenceLength, Tint), null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
	{
		int? nextTimesPlayed = s.FindCard(CardId) is { } card ? ModEntry.Instance.Api.V2.TimesPlayed.GetTimesPlayed(card, (IKokoroApi.IV2.ITimesPlayedApi.Interval)(int)Interval) + 1 : null;
		var step = ((nextTimesPlayed ?? 1) - 1) % SequenceLength + 1;
		var iconStep = (SequenceStep - step + SequenceLength) % SequenceLength + 1;

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
				Icon = SequenceManager.ObtainIcon(Interval, iconStep, SequenceLength, Tint),
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

	public IKokoroApi.IV2.ISequenceApi.ISequenceAction SetTint(Color? value)
	{
		Tint = value;
		return this;
	}
}