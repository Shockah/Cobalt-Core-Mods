using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IOnExhaustApi OnExhaust { get; } = new OnExhaustApi();
		
		public sealed class OnExhaustApi : IKokoroApi.IV2.IOnExhaustApi
		{
			public IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction;

			public IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction MakeAction(CardAction action)
				=> new OnExhaustManager.TriggerAction { Action = action };
		}
	}
}

internal sealed class OnExhaustManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly OnExhaustManager Instance = new();
	
	private static ISpriteEntry ActionIcon = null!;

	internal static void Setup(IHarmony harmony)
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/OnExhaust.png"));
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToExhaust_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
		=> args.Action is TriggerAction triggerAction ? [triggerAction.Action] : null;

	private static void Combat_SendCardToExhaust_Postfix(Combat __instance, State s, Card card)
	{
		var meta = card.GetMeta();
		if (s.CharacterIsMissing(meta.deck))
			return;

		__instance.QueueImmediate(
			card.GetActionsOverridden(s, __instance)
				.Where(action => !action.disabled)
				.OfType<TriggerAction>()
				.Select(triggerAction => triggerAction.Action)
				.Select(action =>
				{
					action.whoDidThis = meta.deck;
					return action;
				})
		);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not TriggerAction triggerAction)
			return true;

		if (!triggerAction.ShowOnExhaustIcon)
		{
			__result = Card.RenderAction(g, state, triggerAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			return false;
		}

		var oldActionDisabled = triggerAction.Action.disabled;
		triggerAction.Action.disabled = triggerAction.disabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ActionIcon.Sprite, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += 10;

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, triggerAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		triggerAction.Action.disabled = oldActionDisabled;

		return false;
	}

	internal sealed class TriggerAction : CardAction, IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction
	{
		public required CardAction Action { get; set; }
		public bool ShowOnExhaustIcon { get; set; } = true;
		public bool ShowOnExhaustTooltip { get; set; } = true;

		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. ShowOnExhaustTooltip ? [new GlossaryTooltip($"action.{GetType().Namespace!}::OnExhaust")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["onExhaust", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["onExhaust", "description"]),
				}] : new List<Tooltip>(),
				.. Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
		
		public IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction SetAction(CardAction value)
		{
			this.Action = value;
			return this;
		}

		public IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction SetShowOnExhaustIcon(bool value)
		{
			this.ShowOnExhaustIcon = value;
			return this;
		}

		public IKokoroApi.IV2.IOnExhaustApi.IOnExhaustAction SetShowOnExhaustTooltip(bool value)
		{
			this.ShowOnExhaustTooltip = value;
			return this;
		}
	}
}