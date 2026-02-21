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
		public IKokoroApi.IV2.IImpulsiveApi Spontaneous
			=> Impulsive;

		public IKokoroApi.IV2.IImpulsiveApi Impulsive { get; } = new ImpulsiveApi();
		
		public sealed class ImpulsiveApi : IKokoroApi.IV2.IImpulsiveApi
		{
			public ICardTraitEntry ImpulsiveTriggeredTrait
				=> SpontaneousManager.ImpulsiveTriggeredTrait;
			
			public ICardTraitEntry SpontaneousTriggeredTrait
				=> SpontaneousManager.ImpulsiveTriggeredTrait;

			public IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction;

			public IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction MakeAction(CardAction action)
				=> new SpontaneousManager.TriggerAction { Action = action };
		}
	}
}

internal sealed class SpontaneousManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly SpontaneousManager Instance = new();
	
	private static ISpriteEntry ActionIcon = null!;
	internal static ICardTraitEntry ImpulsiveTriggeredTrait { get; private set; } = null!;

	internal static void Setup(IHarmony harmony)
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Impulsive.png"));
		var triggeredIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ImpulsiveTriggered.png"));

		ImpulsiveTriggeredTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Impulsive", new()
		{
			Icon = (_, _) => triggeredIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Impulsive"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Impulsive")
				{
					Icon = triggeredIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Impulsive", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Impulsive", "description"]),
				}
			]
		});

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToHand_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SaveThemFromTheVoid)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SaveThemFromTheVoid_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
		{
			QueueSpontaneousActions(state, combat, combat.hand);
		}, -1000);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnEnd), (State state) =>
		{
			foreach (var card in state.GetAllCards())
				if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ImpulsiveTriggeredTrait))
					ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ImpulsiveTriggeredTrait, null, permanent: false);
		});
	}

	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
		=> args.Action is TriggerAction triggerAction ? [triggerAction.Action] : null;

	private static void QueueSpontaneousActions(State state, Combat combat, IEnumerable<Card> cards)
		=> combat.Queue(
			cards
				.Where(card => !state.CharacterIsMissing(card.GetMeta().deck))
				.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ImpulsiveTriggeredTrait))
				.Select(card => (Card: card, Actions: card.GetActionsOverridden(state, combat).Where(action => !action.disabled).OfType<TriggerAction>().Select(triggerAction => triggerAction.Action).ToList()))
				.Where(e => e.Actions.Count != 0)
				.Select(e =>
				{
					ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, e.Card, ImpulsiveTriggeredTrait, true, permanent: false);
					return e;
				})
				.SelectMany(e =>
				{
					var meta = e.Card.GetMeta();
					return e.Actions.Select(action =>
					{
						action.whoDidThis = meta.deck;
						ModEntry.Instance.Helper.ModData.SetModData(action, "Impulsive", true);
						return action;
					});
				})
		);

	private static void Combat_SendCardToHand_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.hand.Contains(card))
			return;
		if (__instance.turn < 1)
			return;

		QueueSpontaneousActions(s, __instance, [card]);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not TriggerAction triggerAction)
			return true;

		if (!triggerAction.ShowImpulsiveIcon)
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

	private static void Combat_SaveThemFromTheVoid_Prefix(Combat __instance, Deck d)
		=> __instance.QueueImmediate(new RequeueActionsAfterSavingFromTheVoidAction { Deck = d });

	internal sealed class TriggerAction : CardAction, IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction
	{
		public required CardAction Action { get; set; }
		public bool ShowImpulsiveIcon { get; set; } = true;
		public bool ShowImpulsiveTooltip { get; set; } = true;

		[JsonIgnore]
		public CardAction AsCardAction
			=> this;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. ShowImpulsiveTooltip ? [new GlossaryTooltip($"action.{GetType().Namespace!}::Impulsive")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["impulsive", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["impulsive", "description"]),
				}] : new List<Tooltip>(),
				.. Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
		
		public IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction SetAction(CardAction value)
		{
			this.Action = value;
			return this;
		}

		public IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction SetShowImpulsiveIcon(bool value)
		{
			this.ShowImpulsiveIcon = value;
			return this;
		}

		public IKokoroApi.IV2.IImpulsiveApi.IImpulsiveAction SetShowImpulsiveTooltip(bool value)
		{
			this.ShowImpulsiveTooltip = value;
			return this;
		}
	}

	private sealed class RequeueActionsAfterSavingFromTheVoidAction : CardAction
	{
		public required Deck Deck;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.CharacterIsMissing(Deck))
				return;

			QueueSpontaneousActions(s, c, c.hand.Where(card => card.GetMeta().deck == Deck));
		}
	}
}