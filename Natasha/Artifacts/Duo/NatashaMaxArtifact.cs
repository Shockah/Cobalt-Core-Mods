using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Natasha;

internal sealed class NatashaMaxArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	[JsonProperty]
	private bool TriggeredThisTurn;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Max.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/MaxInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("NatashaMax", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Max", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Max", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.hacker]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.BeginCardAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler)), priority: Priority.Last + 1)
		);
	}

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TriggeredThisTurn = false;
	}

	private static HashSet<int> GetLastExhaustCardIds(Combat combat, out bool isNewList)
	{
		isNewList = !ModEntry.Instance.Helper.ModData.ContainsModData(combat, "LastExhaustCardIds");
		return ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "LastExhaustCardIds");
	}

	private static void UpdateLastExhaustCardIds(Combat combat, HashSet<int>? lastExhaustCardIds = null)
	{
		lastExhaustCardIds ??= GetLastExhaustCardIds(combat, out _);
		lastExhaustCardIds.Clear();
		foreach (var card in combat.exhausted)
			lastExhaustCardIds.Add(card.uuid);
	}

	private static void TriggerArtifact(State state, Combat combat)
	{
		if (state.EnumerateAllArtifacts().OfType<NatashaMaxArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisTurn)
			return;
		
		var lastExhaustCardIds = GetLastExhaustCardIds(combat, out var isNewList);
		if (!isNewList)
		{
			for (var i = 0; i < combat.exhausted.Count; i++)
			{
				if (combat.hand.Count == 0)
					break;
			
				var exhaustedCard = combat.exhausted[i];
				if (lastExhaustCardIds.Contains(exhaustedCard.uuid))
					continue;

				var handCard = combat.hand[0];
				combat.hand.RemoveAt(0);
				handCard.ExhaustFX();
				combat.SendCardToExhaust(state, handCard);
			
				combat.exhausted.RemoveAt(i);
				combat.SendCardToHand(state, exhaustedCard, 0);
				artifact.TriggeredThisTurn = true;
				artifact.Pulse();
				break;
			}
		}
		
		UpdateLastExhaustCardIds(combat, lastExhaustCardIds);
	}

	private static void Combat_BeginCardAction_Prefix(Combat __instance)
		=> UpdateLastExhaustCardIds(__instance);

	private static void Combat_TryPlayCard_Prefix(Combat __instance)
		=> UpdateLastExhaustCardIds(__instance);

	private static void Combat_TryPlayCard_Postfix(Combat __instance, State s)
		=> TriggerArtifact(s, __instance);
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_DrainCardActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Instruction(OpCodes.Ldnull),
					ILMatches.Stfld("currentCardAction"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_OnCurrentCardActionClear))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Combat_DrainCardActions_Transpiler_OnCurrentCardActionClear(Combat combat, G g)
		=> TriggerArtifact(g.state, combat);
}