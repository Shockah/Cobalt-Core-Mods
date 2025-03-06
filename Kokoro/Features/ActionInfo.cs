using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nickel;

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
		}
	}
}

internal sealed class ActionInfoManager
{
	internal static readonly ActionInfoManager Instance = new();

	private static Card? CardBeingRendered;
	
	private ActionInfoManager()
	{
	}
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix_Last)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Finalizer))
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
	
	private static void Card_GetActionsOverridden_Postfix_Last(Card __instance, ref List<CardAction> __result)
	{
		if (CardBeingRendered is not null)
			return;
		
		// TODO: switch to using a transpiler, to make it work with Soggins etc
		foreach (var action in __result.SelectMany(a => WrappedActionManager.Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: true)))
			SetSourceCard(action, __instance);
	}

	private static void Card_Render_Prefix(Card __instance)
		=> CardBeingRendered = __instance;

	private static void Card_Render_Finalizer()
		=> CardBeingRendered = null;
}