using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shockah.Kokoro;

namespace Shockah.Bloch;

internal sealed class MuscleMemoryArtifact : Artifact, IRegisterable
{
	private static Card? LastCardPlayed;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("MuscleMemory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/MuscleMemory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToDiscard_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new ADummyAction()).AsCardAction.GetTooltips(DB.fakeState);

	private static void Combat_TryPlayCard_Prefix(Card card)
		=> LastCardPlayed = card;

	private static void Combat_TryPlayCard_Finalizer()
		=> LastCardPlayed = null;

	private static void Combat_SendCardToDiscard_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.isPlayerTurn)
			return;
		if (card == LastCardPlayed)
			return;

		var meta = card.GetMeta();
		if (s.CharacterIsMissing(meta.deck))
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is MuscleMemoryArtifact) is not { } artifact)
			return;

		var actions = card.GetActionsOverridden(s, __instance)
			.Where(action => !action.disabled)
			.Select(action => ModEntry.Instance.KokoroApi.OnDiscard.AsAction(action))
			.OfType<IKokoroApi.IV2.IOnDiscardApi.IOnDiscardAction>()
			.Select(triggerAction => triggerAction.Action)
			.Select(action =>
			{
				action.whoDidThis = meta.deck;
				return action;
			})
			.ToList();

		if (actions.Count == 0)
			return;

		if (string.IsNullOrEmpty(actions[0].artifactPulse))
			actions[0].artifactPulse = artifact.Key();
		else
			artifact.Pulse();

		__instance.QueueImmediate(actions);
	}
}