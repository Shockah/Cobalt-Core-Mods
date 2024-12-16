using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public CardAction MakeSpoofed(CardAction renderAction, CardAction realAction)
			=> new ASpoofed { RenderAction = renderAction, RealAction = realAction };
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.ISpoofedActionsApi SpoofedActions { get; } = new SpoofedActionsApi();
		
		public sealed class SpoofedActionsApi : IKokoroApi.IV2.ISpoofedActionsApi
		{
			public IKokoroApi.IV2.ISpoofedActionsApi.ISpoofedAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.ISpoofedActionsApi.ISpoofedAction;

			public IKokoroApi.IV2.ISpoofedActionsApi.ISpoofedAction MakeAction(CardAction renderAction, CardAction realAction)
				=> new ASpoofed { RenderAction = renderAction, RealAction = realAction };
		}
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

		__result = Card.RenderAction(g, state, spoofedAction.RenderAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		return false;
	}
}

public sealed class ASpoofed : CardAction, IKokoroApi.IV2.ISpoofedActionsApi.ISpoofedAction
{
	public required CardAction RenderAction { get; set; }
	public required CardAction RealAction { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		RealAction.whoDidThis = whoDidThis;
		c.QueueImmediate(RealAction);
	}

	public override Icon? GetIcon(State s)
		=> RenderAction.GetIcon(s);

	public override List<Tooltip> GetTooltips(State s)
		=> RenderAction.omitFromTooltips ? [] : RenderAction.GetTooltips(s);

	public override bool CanSkipTimerIfLastEvent()
		=> RealAction.CanSkipTimerIfLastEvent();
	
	public IKokoroApi.IV2.ISpoofedActionsApi.ISpoofedAction SetRenderAction(CardAction value)
	{
		RenderAction = value;
		return this;
	}

	public IKokoroApi.IV2.ISpoofedActionsApi.ISpoofedAction SetRealAction(CardAction value)
	{
		RealAction = value;
		return this;
	}
}