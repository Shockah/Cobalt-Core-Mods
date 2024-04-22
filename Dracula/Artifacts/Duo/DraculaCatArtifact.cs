using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaCatArtifact : Artifact, IRegisterable
{
	private static readonly HashSet<MethodInfo> IsDuringExeCardOfferingMethods = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DraculaCat", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaCat.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaCat", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaCat", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, Deck.colorless]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatchVirtual(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ACardOffering), nameof(ACardOffering.BeginWithRoute)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [new TTCard { card = new GrimoireOfSecretsCard() }];

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, ref List<CardAction> __result)
	{
		if (!ModEntry.Instance.IsExeCardType(__instance.GetType()))
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaCatArtifact) is not { } artifact)
			return;

		foreach (var baseAction in __result)
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.Actions.GetWrappedCardActionsRecursively(baseAction))
				if (wrappedAction is ACardOffering)
					ModEntry.Instance.Helper.ModData.SetModData(wrappedAction, "IsExeCardOffering", true);
	}

	private static void ACardOffering_BeginWithRoute_Prefix(ACardOffering __instance, State s, MethodInfo __originalMethod)
	{
		if (!ModEntry.Instance.Helper.ModData.TryGetModData(__instance, "IsExeCardOffering", out bool isExeCardOffering) || !isExeCardOffering)
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaCatArtifact) is not { } artifact)
			return;

		IsDuringExeCardOfferingMethods.Add(__originalMethod);
	}

	private static void ACardOffering_BeginWithRoute_Finalizer(MethodInfo __originalMethod)
		=> IsDuringExeCardOfferingMethods.Remove(__originalMethod);

	private static void CardReward_GetOffering_Postfix(State s, ref List<Card> __result)
	{
		if (IsDuringExeCardOfferingMethods.Count == 0)
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaCatArtifact) is not { } artifact)
			return;

		artifact.Pulse();

		if (__result.Count >= 7)
			__result.RemoveAt(s.rngCardOfferings.NextInt() % __result.Count);

		List<Type> secretTypes = [..ModEntry.SecretAttackCardTypes, ..ModEntry.SecretNonAttackCardTypes];
		var secretType = secretTypes[s.rngCardOfferings.NextInt() % secretTypes.Count];
		var secretCard = (Card)Activator.CreateInstance(secretType)!;
		secretCard.drawAnim = 1;
		secretCard.flipAnim = 1;
		__result.Add(secretCard);
	}
}