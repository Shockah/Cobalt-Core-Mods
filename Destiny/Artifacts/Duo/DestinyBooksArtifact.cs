using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyBooksArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private bool LostShards;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyBooks", new()
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

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, Deck.shard]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.BubbleEvents)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_BubbleEvents_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_BubbleEvents_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.shard, (MG.inst.g?.state ?? DB.fakeState).ship.GetMaxShard()),
			.. StatusMeta.GetTooltips(MagicFind.MagicFindStatus.Status, 1),
		];
	
	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LostShards = false;
	}

	public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
	{
		base.OnQueueEmptyDuringPlayerTurn(state, combat);
		if (!LostShards)
			return;

		LostShards = false;

		if (state.ship.Get(MagicFind.MagicFindStatus.Status) <= 0)
			return;
		if (state.ship.Get(Status.shard) >= state.ship.GetMaxShard())
			return;
		
		combat.QueueImmediate([
			new AStatus
			{
				targetPlayer = true,
				status = MagicFind.MagicFindStatus.Status,
				statusAmount = -1,
				artifactPulse = Key(),
				timer = 0,
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.shard,
				statusAmount = 1,
			},
		]);
	}

	private static void Combat_Update_Prefix(G g, out int __state)
		=> __state = g.state.ship.Get(Status.shard);

	private static void Combat_Update_Postfix(Combat __instance, G g, in int __state)
	{
		if (!__instance.isPlayerTurn)
			return;
		
		var shardsLost = __state - g.state.ship.Get(Status.shard);
		if (shardsLost <= 0)
			return;
		
		if (g.state.EnumerateAllArtifacts().OfType<DestinyBooksArtifact>().FirstOrDefault() is not { } artifact)
			return;

		artifact.LostShards = true;
	}

	private static void G_BubbleEvents_Prefix(G __instance, out int __state)
		=> __state = __instance.state.ship.Get(Status.shard);

	private static void G_BubbleEvents_Postfix(G __instance, in int __state)
	{
		if (__instance.state.route is not Combat combat)
			return;
		if (!combat.isPlayerTurn)
			return;
		
		var shardsLost = __state - combat.energy;
		if (shardsLost <= 0)
			return;
		
		if (__instance.state.EnumerateAllArtifacts().OfType<DestinyBooksArtifact>().FirstOrDefault() is not { } artifact)
			return;
		
		artifact.LostShards = true;
	}
}