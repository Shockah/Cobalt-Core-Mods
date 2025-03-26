using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using TheJazMaster.Bucket;

namespace Shockah.Destiny;

internal sealed class DestinyBucketArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	private const int Threshold = 5;
	
	[JsonProperty]
	private int Counter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.Helper.ModRegistry.GetApi<IBucketApi>("TheJazMaster.Bucket") is not { } bucketApi)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyBucket", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Bucket.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Bucket", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Bucket", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, bucketApi.BucketDeck]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToHand_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null)?.ToList();

	public override int? GetDisplayNumber(State s)
		=> Counter;

	private static void Combat_SendCardToHand_Postfix(Combat __instance, State s, Card card)
	{
		if (card.GetMeta().deck != Deck.trash)
			return;
		if (!__instance.hand.Contains(card))
			return;
		if (s.EnumerateAllArtifacts().OfType<DestinyBucketArtifact>().FirstOrDefault() is not { } artifact)
			return;

		artifact.Counter++;
		if (artifact.Counter < Threshold)
			return;

		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Explosive.ExplosiveTrait))
			return;

		artifact.Counter = 0;
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, Explosive.ExplosiveTrait, true, false);
		artifact.Pulse();
	}
}