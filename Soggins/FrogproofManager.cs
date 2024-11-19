using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Soggins;

internal sealed class FrogproofManager : HookManager<IFrogproofHook>
{
	private static ModEntry Instance => ModEntry.Instance;

	public FrogproofManager() : base(Instance.Package.Manifest.UniqueName)
	{
		Register(FrogproofCardTraitFrogproofHook.Instance, 0);
		Register(NonVanillaNonCharacterCardFrogproofHook.Instance, 1);
		Register(FrogproofingFrogproofHook.Instance, -10);
	}

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(Ship_Set_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.EndRun)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(State_EndRun_Postfix))
		);
	}

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> GetFrogproofType(state, combat, card, context) != FrogproofType.None;

	public FrogproofType GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
	{
		foreach (var hook in GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.GetFrogproofType(state, combat, card, context);
			if (hookResult == FrogproofType.None)
				return FrogproofType.None;
			if (hookResult != null)
				return hookResult.Value;
		}
		return FrogproofType.None;
	}

	public IFrogproofHook? GetHandlingHook(State state, Combat? combat, Card card, FrogproofHookContext context = FrogproofHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.GetFrogproofType(state, combat, card, context);
			if (hookResult == FrogproofType.None)
				return null;
			if (hookResult != null)
				return hook;
		}
		return null;
	}

	private static void Ship_Set_Postfix(Ship __instance, Status status, int n)
	{
		if (MG.inst.g.state is not { } state || state.ship != __instance)
			return;
		if (n == 0)
			return;
		if (status != (Status)Instance.SmugStatus.Id!.Value)
			return;
		Instance.Api.SetSmugEnabled(state, __instance);
	}

	private static void State_EndRun_Postfix(State __instance)
		=> Instance.Helper.ModData.RemoveModData(__instance, ApiImplementation.IsRunWithSmugKey);
}

public sealed class FrogproofCardTraitFrogproofHook : IFrogproofHook
{
	public static FrogproofCardTraitFrogproofHook Instance { get; private set; } = new();

	private FrogproofCardTraitFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> ModEntry.Instance.Helper.Content.Cards.GetCardTraitState(state, card, ModEntry.Instance.FrogproofTrait).IsActive ? FrogproofType.Innate : null;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class NonVanillaNonCharacterCardFrogproofHook : IFrogproofHook
{
	public static NonVanillaNonCharacterCardFrogproofHook Instance { get; private set; } = new();

	private NonVanillaNonCharacterCardFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
	{
		var meta = card.GetMeta();
		if (NewRunOptions.allChars.Contains(meta.deck))
			return null;
		if (meta.deck is Deck.colorless or Deck.catartifact or Deck.soggins or Deck.dracula or Deck.tooth or Deck.ephemeral)
			return null;
		return FrogproofType.InnateHiddenIfNotNeeded;
	}

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class FrogproofingFrogproofHook : IFrogproofHook
{
	public static FrogproofingFrogproofHook Instance { get; private set; } = new();

	private FrogproofingFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> context == FrogproofHookContext.Action && state.ship.Get((Status)ModEntry.Instance.FrogproofingStatus.Id!.Value) > 0 ? FrogproofType.Paid : null;

	public void PayForFrogproof(State state, Combat? combat, Card card)
		=> state.ship.Add((Status)ModEntry.Instance.FrogproofingStatus.Id!.Value, -1);
}