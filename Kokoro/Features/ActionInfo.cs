using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nickel;
using Shockah.Shared;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IActionInfoApi ActionInfo { get; } = new ActionInfoApi();
		
		public sealed class ActionInfoApi : IKokoroApi.IV2.IActionInfoApi
		{
			public int? GetSourceCardId(CardAction action)
				=> ActionInfoManager.GetSourceCardId(action);

			public Card? GetSourceCard(State state, CardAction action)
				=> ActionInfoManager.GetSourceCard(state, action);

			public void SetSourceCardId(CardAction action, int? sourceId)
				=> ActionInfoManager.SetSourceCardId(action, sourceId);

			public void SetSourceCard(CardAction action, State state, Card? source)
				=> ActionInfoManager.SetSourceCard(action, source);

			public Guid? GetInteractionId(CardAction action)
				=> ActionInfoManager.GetInteractionId(action);

			public void SetInteractionId(CardAction action, Guid? interactionId)
				=> ActionInfoManager.SetInteractionId(action, interactionId);

			public void RegisterHook(IKokoroApi.IV2.IActionInfoApi.IHook hook, double priority = 0)
				=> ActionInfoManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IActionInfoApi.IHook hook)
				=> ActionInfoManager.Instance.Unregister(hook);
		}
	}
}

internal sealed class ActionInfoManager : HookManager<IKokoroApi.IV2.IActionInfoApi.IHook>
{
	internal static readonly ActionInfoManager Instance = new();
	
	private ActionInfoManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix_Last)), priority: Priority.Last)
		);
	}

	internal static int? GetSourceCardId(CardAction action)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(action, "SourceCardId");

	internal static Card? GetSourceCard(State state, CardAction action)
	{
		if (GetSourceCardId(action) is not { } cardId)
			return null;
		return state.FindCard(cardId);
	}
	
	internal static void SetSourceCardId(CardAction action, int? sourceCardId)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(action, "SourceCardId", sourceCardId);
	
	internal static void SetSourceCard(CardAction action, Card? sourceCard)
		=> SetSourceCardId(action, sourceCard?.uuid);
	
	internal static Guid? GetInteractionId(CardAction action)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<Guid>(action, "InteractionId");
	
	internal static void SetInteractionId(CardAction action, Guid? interactionId)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(action, "InteractionId", interactionId);
	
	private static void Card_GetActionsOverridden_Postfix_Last(Card __instance, ref List<CardAction> __result)
	{
		var interactionEndedAction = new InteractionEndedAction();
		var hiddenAction = new AHidden { Action = interactionEndedAction };
		__result.Add(hiddenAction);
		
		var guid = Guid.NewGuid();
		foreach (var action in __result.SelectMany(a => WrappedActionManager.Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: true)))
		{
			SetSourceCard(action, __instance);
			SetInteractionId(action, guid);
		}
	}

	private sealed class InteractionEndedAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
	}
}