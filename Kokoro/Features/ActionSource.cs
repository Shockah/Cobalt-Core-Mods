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
		public IKokoroApi.IV2.IActionSourceApi ActionSource { get; } = new ActionSourceApi();
		
		public sealed class ActionSourceApi : IKokoroApi.IV2.IActionSourceApi
		{
			public int? GetSourceCardId(CardAction action)
				=> ActionSourceManager.GetSourceCardId(action);

			public Card? GetSourceCard(State state, CardAction action)
				=> ActionSourceManager.GetSourceCard(state, action);

			public void SetSourceCardId(CardAction action, int? sourceId)
				=> ActionSourceManager.SetSourceCardId(action, sourceId);

			public void SetSourceCard(CardAction action, State state, Card? source)
				=> ActionSourceManager.SetSourceCard(action, source);
		}
	}
}

internal static class ActionSourceManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
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
	
	private static void Card_GetActionsOverridden_Postfix(Card __instance, ref List<CardAction> __result)
	{
		foreach (var action in __result.SelectMany(a => WrappedActionManager.Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: true)))
			SetSourceCard(action, __instance);
	}
}