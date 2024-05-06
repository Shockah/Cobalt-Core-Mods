using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class SpontaneousManager : IWrappedActionHook
{
	private static ISpriteEntry ActionIcon = null!;
	private static ISpriteEntry TriggeredIcon = null!;
	internal static ICardTraitEntry SpontaneousTriggeredTrait { get; private set; } = null!;

	public SpontaneousManager()
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/Spontaneous.png"));
		TriggeredIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/SpontaneousTriggered.png"));

		SpontaneousTriggeredTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Spontaneous", new()
		{
			Icon = (_, _) => TriggeredIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Spontaneous"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{GetType().Namespace!}::Spontaneous")
				{
					Icon = TriggeredIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Spontaneous", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Spontaneous", "description"]),
				}
			]
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_SendCardToHand_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
		{
			if (!combat.isPlayerTurn)
				return;

			QueueSpontaneousActions(state, combat, combat.hand);
		}, -1000);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnEnd), (State state, Combat combat) =>
		{
			if (combat.isPlayerTurn)
				return;

			foreach (var card in state.GetAllCards())
				if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, SpontaneousTriggeredTrait))
					ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, SpontaneousTriggeredTrait, null, permanent: false);
		}, 0);

		ModEntry.Instance.KokoroApi.Actions.RegisterWrappedActionHook(this, 0);
	}

	private static void QueueSpontaneousActions(State state, Combat combat, IEnumerable<Card> cards)
	{
		var firstNonSpontanenousIndex = combat.cardActions.FindIndex(action => !ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(action, "Spontaneous"));
		var indexToInsertAt = firstNonSpontanenousIndex < 0 ? 0 : firstNonSpontanenousIndex;

		combat.cardActions.InsertRange(
			indexToInsertAt,
			cards
				.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, SpontaneousTriggeredTrait))
				.Select(card => (Card: card, Actions: card.GetActionsOverridden(state, combat).Where(action => !action.disabled).OfType<TriggerAction>().Select(triggerAction => triggerAction.Action).ToList()))
				.Where(e => e.Actions.Count != 0)
				.SelectMany(e =>
				{
					var meta = e.Card.GetMeta();
					return e.Actions
						.Prepend(new MarkCardAsTriggeredAction { CardId = e.Card.uuid })
						.Select(action =>
						{
							action.whoDidThis = meta.deck;
							ModEntry.Instance.Helper.ModData.SetModData(action, "Spontaneous", true);
							return action;
						});
				})
		);
	}

	private static void Combat_SendCardToHand_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.hand.Contains(card))
			return;

		QueueSpontaneousActions(s, __instance, [card]);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not TriggerAction triggerAction)
			return true;

		bool oldActionDisabled = triggerAction.Action.disabled;
		triggerAction.Action.disabled = triggerAction.disabled;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

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

	public List<CardAction>? GetWrappedCardActions(CardAction action)
		=> action is TriggerAction triggerAction ? [triggerAction.Action] : null;

	internal sealed class TriggerAction : CardAction
	{
		public required CardAction Action;

		public override Icon? GetIcon(State s)
			=> new(ActionIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{GetType().Namespace!}::Spontaneous")
				{
					Icon = ActionIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "Spontaneous", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "Spontaneous", "description"]),
				},
				..Action.GetTooltips(s)
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is UnlockedPotentialArtifact) is not { } artifact)
				return;

			if (string.IsNullOrEmpty(Action.artifactPulse))
				Action.artifactPulse = artifact.Key();
			c.QueueImmediate(Action);
		}
	}

	internal sealed class MarkCardAsTriggeredAction : CardAction
	{
		public required int CardId;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.FindCard(CardId) is not { } card)
				return;
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, SpontaneousTriggeredTrait, true, permanent: false);
		}
	}
}