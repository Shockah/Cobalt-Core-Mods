using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class JohnsonBucketArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	public bool TriggeredThisTurn { get; set; } = false;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (helper.Content.Decks.LookupByUniqueName("TheJazMaster.Bucket::Bucket") is not { } bucketDeck)
			return;

		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/JohnsonBucket.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/JohnsonBucketInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("JohnsonBucket", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonBucket", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonBucket", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.JohnsonDeck.Deck, bucketDeck.Deck]);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToHand_Postfix))
		);
	}

	public override Spr GetSprite()
		=> (TriggeredThisTurn ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip>? GetExtraTooltips()
		=> [new TTGlossary("cardtrait.discount", 1)];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.isPlayerTurn)
			TriggeredThisTurn = false;
	}

	private static void Combat_SendCardToHand_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.hand.Contains(card))
			return;
		if (card.GetMeta().deck != Deck.trash)
			return;
		if (s.EnumerateAllArtifacts().OfType<JohnsonBucketArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisTurn)
			return;

		artifact.TriggeredThisTurn = true;
		__instance.Queue(new Action
		{
			CardId = card.uuid,
			artifactPulse = artifact.Key()
		});
	}

	private sealed class Action : CardAction
	{
		public required int CardId;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (s.FindCard(CardId) is not { } card)
				return;

			card.discount -= 1;
			Audio.Play(Event.CardHandling);
		}
	}
}