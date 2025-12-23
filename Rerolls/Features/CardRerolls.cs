using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Rerolls;

internal sealed class CardRerollManager
{
	private static readonly UK RerollButtonUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	private static ACardOffering? ActionContext;

	public CardRerollManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ACardOffering), nameof(ACardOffering.BeginWithRoute)),
			prefix: new HarmonyMethod(GetType(), nameof(ACardOffering_BeginWithRoute_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(ACardOffering_BeginWithRoute_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => typeof(CardReward).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<GetOffering>") && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(GetType(), nameof(CardReward_GetOffering_Delegate_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.Render)),
			postfix: new HarmonyMethod(GetType(), nameof(CardReward_Render_Postfix))
		);
	}

	private static bool AreRerollsPossible(G g, out RerollArtifact? artifact)
	{
		artifact = null;
		if (FeatureFlags.Debug && Input.shift)
			return true;
		if (g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault() is { } existingArtifact && existingArtifact.RerollsLeft > 0)
		{
			artifact = existingArtifact;
			return true;
		}
		return false;
	}

	private static void Reroll(G g, CardReward route)
	{
		if (!AreRerollsPossible(g, out var artifact))
			return;
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<ACardOffering>(route, "OriginalAction") is not { } originalAction)
			return;

		if (artifact is not null)
		{
			artifact.RerollsLeft--;
			artifact.Pulse();
		}

		var newAction = Mutil.DeepCopy(originalAction);

		var rerolledCards = ModEntry.Instance.Helper.ModData.ObtainModData(originalAction, "RerolledCards", () => new HashSet<string>());
		foreach (var cardChoice in route.cards)
			rerolledCards.Add(cardChoice.Key());

		g.state.GetCurrentQueue().QueueImmediate(newAction);
		g.CloseRoute(route);
	}

	private static void ACardOffering_BeginWithRoute_Prefix(ACardOffering __instance)
		=> ActionContext = __instance;

	private static void ACardOffering_BeginWithRoute_Finalizer(ACardOffering __instance, State s, Combat c, Route? __result)
	{
		ActionContext = null;

		if (s.route == c)
			return;
		if (__result is not CardReward route)
			return;

		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "OriginalAction", __instance);
	}

	private static void CardReward_GetOffering_Delegate_Postfix(Card c, ref bool __result)
	{
		if (!__result)
			return;
		if (ActionContext is not { } action)
			return;
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<HashSet<string>>(action, "RerolledCards") is not { } rerolledCards)
			return;

		if (rerolledCards.Contains(c.Key()))
			__result = false;
	}

	private static void CardReward_Render_Postfix(CardReward __instance, G g)
	{
		if (__instance.ugpradePreview is not null)
			return;
		if (!AreRerollsPossible(g, out _))
			return;
		if (!ModEntry.Instance.Helper.ModData.ContainsModData(__instance, "OriginalAction"))
			return;

		SharedArt.ButtonText(
			g,
			new Vec(210, 228),
			RerollButtonUk,
			ModEntry.Instance.Localizations.Localize(["button"]),
			onMouseDown: new MouseDownHandler(() => Reroll(g, __instance)),
			platformButtonHint: Btn.Y
		);
		if (g.boxes.FirstOrDefault(b => b.key == RerollButtonUk) is { } box)
			box.onInputPhase = new InputPhaseHandler(() =>
			{
				if (Input.GetGpDown(Btn.Y))
					Reroll(g, __instance);
			});
	}
}
