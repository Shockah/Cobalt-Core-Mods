using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Natasha;

internal sealed class NatashaMaxArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	private static Card? IsPlayingCard;
	
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
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToExhaust_Prefix_HighPriority)), priority: Priority.High)
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTGlossary("cardtrait.exhaust")];

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TriggeredThisTurn = false;
	}

	private static void Combat_TryPlayCard_Prefix(Card card)
		=> IsPlayingCard = card;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsPlayingCard = null;

	private static void Combat_SendCardToExhaust_Prefix_HighPriority(Combat __instance, State s, ref Card card)
	{
		if (__instance.hand.Count == 0)
			return;
		if (__instance.hand[0] == card)
			return;
		if (s.EnumerateAllArtifacts().OfType<NatashaMaxArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisTurn)
			return;
		
		var handCard = __instance.hand[0];
		__instance.hand.RemoveAt(0);
		handCard.ExhaustFX();
		
		s.RemoveCardFromWhereverItIs(card.uuid);
		__instance.SendCardToHand(s, card, IsPlayingCard == card ? null : 0);
		
		card = handCard;
		
		artifact.TriggeredThisTurn = true;
		artifact.Pulse();
	}
}