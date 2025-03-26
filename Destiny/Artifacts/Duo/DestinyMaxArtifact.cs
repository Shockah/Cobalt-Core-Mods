using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyMaxArtifact : Artifact, IRegisterable, IDestinyApi.IHook
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
		
		helper.Content.Artifacts.RegisterArtifact("DestinyMax", new()
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

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, Deck.hacker]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToExhaust_Postfix))
		);
	}

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. StatusMeta.GetTooltips(Status.shard, (MG.inst.g?.state ?? DB.fakeState).ship.GetMaxShard()),
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TriggeredThisTurn = false;
	}

	private static void Combat_SendCardToExhaust_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.exhausted.Contains(card))
			return;
		if (s.EnumerateAllArtifacts().OfType<DestinyMaxArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisTurn)
			return;
		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait))
			return;

		artifact.TriggeredThisTurn = true;
		__instance.QueueImmediate(new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 1, artifactPulse = artifact.Key() });
	}
}