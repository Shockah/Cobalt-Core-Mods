using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public bool TryGetSpoofedAction(CardAction maybeSpoofedAction, [MaybeNullWhen(false)] out CardAction renderAction, [MaybeNullWhen(false)] out CardAction realAction)
		{
			if (maybeSpoofedAction is not ASpoofed spoofedAction)
			{
				renderAction = null;
				realAction = null;
				return false;
			}

			renderAction = spoofedAction.RenderAction;
			realAction = spoofedAction.RealAction;
			return true;
		}
		
		public CardAction MakeSpoofed(CardAction renderAction, CardAction realAction)
			=> new ASpoofed { RenderAction = renderAction, RealAction = realAction };
	}
}

internal sealed class SpoofedActionManager : IWrappedActionHook
{
	internal static readonly HiddenActionManager Instance = new();
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}
	
	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not ASpoofed spoofed)
			return null;

		List<CardAction> results = [];
		if (spoofed.RenderAction is { } renderAction)
			results.Add(renderAction);
		if (spoofed.RealAction is { } realAction)
			results.Add(realAction);
		return results.Count == 0 ? null : results;
	}
	
	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not ASpoofed spoofedAction)
			return true;
		if ((spoofedAction.RenderAction ?? spoofedAction.RealAction) is not { } actionToRender)
			return true;

		__result = Card.RenderAction(g, state, actionToRender, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		return false;
	}
}

public sealed class ASpoofed : CardAction
{
	public required CardAction RenderAction;
	public required CardAction RealAction;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		RealAction.whoDidThis = whoDidThis;
		c.QueueImmediate(RealAction);
	}

	public override Icon? GetIcon(State s)
		=> RenderAction?.GetIcon(s);

	public override List<Tooltip> GetTooltips(State s)
		=> RenderAction?.omitFromTooltips == true ? [] : (RenderAction?.GetTooltips(s) ?? []);

	public override bool CanSkipTimerIfLastEvent()
		=> RealAction?.CanSkipTimerIfLastEvent() ?? base.CanSkipTimerIfLastEvent();
}