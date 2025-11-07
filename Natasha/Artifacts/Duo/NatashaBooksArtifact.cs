using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Natasha;

internal sealed class NatashaBooksArtifact : Artifact, IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("NatashaBooks", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Books.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Books", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Books", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.shard]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.shard, (MG.inst.g?.state ?? DB.fakeState).ship.GetMaxShard()),
			new TTGlossary("action.spawn"),
			.. new Geode().GetTooltips(),
			.. ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(MG.inst.g?.state ?? DB.fakeState, null) ?? [],
		];

	private void HandleCard(State state, Card card)
	{
		var states = ModEntry.Instance.Helper.Content.Cards.GetAllCardTraits(state, card);
		if (states.TryGetValue(ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait, out var exhaustState) && exhaustState.IsActive)
			return;
		if (states.TryGetValue(ModEntry.Instance.KokoroApi.Limited.Trait, out var limitedState) && limitedState.IsActive)
			return;
		
		Pulse();
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.KokoroApi.Limited.Trait, true, false);
		ModEntry.Instance.KokoroApi.Limited.SetLimitedUses(state, card, 2);
	}

	private static void AStatus_Begin_Prefix(AStatus __instance, State s, out int __state)
		=> __state = __instance.targetPlayer ? s.ship.Get(__instance.status) : 0;

	private static void AStatus_Begin_Postfix(AStatus __instance, State s, in int __state)
	{
		if (!__instance.targetPlayer)
			return;
		if (__instance.status != Status.shard)
			return;
		if (s.ship.Get(__instance.status) <= __state)
			return;
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, __instance) is not { } card)
			return;
		if (s.EnumerateAllArtifacts().OfType<NatashaBooksArtifact>().FirstOrDefault() is not { } artifact)
			return;
		artifact.HandleCard(s, card);
	}

	private static void ASpawn_Begin_Postfix(ASpawn __instance, State s)
	{
		if (!__instance.fromPlayer)
			return;
		if (__instance.thing is not Geode)
			return;
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, __instance) is not { } card)
			return;
		if (s.EnumerateAllArtifacts().OfType<NatashaBooksArtifact>().FirstOrDefault() is not { } artifact)
			return;
		artifact.HandleCard(s, card);
	}

	public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
	{
		if (args.Status != Status.shard)
			return args.NewAmount;
		if (args.NewAmount <= args.OldAmount)
			return args.NewAmount;
		return args.NewAmount + 1;
	}
}