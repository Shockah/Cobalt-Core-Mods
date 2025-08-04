using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class LongTermMemoryArtifact : Artifact, IRegisterable
{
	internal static IArtifactEntry Entry { get; private set; } = null!;
	
	[JsonProperty]
	private int RetainCooldown;

	[JsonProperty]
	private int DiscardCooldown;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("LongTermMemory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/LongTermMemory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LongTermMemory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LongTermMemory", "description", "stateless"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [new TTGlossary("cardtrait.retain")];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		RetainCooldown = 0;
		DiscardCooldown = 0;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		RetainCooldown = Math.Max(RetainCooldown - 1, 0);
		DiscardCooldown = Math.Max(DiscardCooldown - 1, 0);
	}

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
	{
		if (__instance is not LongTermMemoryArtifact artifact)
			return;

		var textTooltip = __result.OfType<TTText>().FirstOrDefault(t => t.text.StartsWith("<c=artifact>"));
		if (textTooltip is null)
			return;

		if (MG.inst.g?.state is not { } state || state.IsOutsideRun())
			return;
		textTooltip.text = DB.Join(
			"<c=artifact>{0}</c>\n".FF(__instance.GetLocName()),
			ModEntry.Instance.Localizations.Localize(["artifact", "LongTermMemory", "description", "stateful"], new
			{
				RetainColor = (artifact.RetainCooldown > 0 ? Colors.disabledText : Colors.textBold).ToString(),
				DiscardColor = (artifact.DiscardCooldown > 0 ? Colors.disabledText : Colors.textBold).ToString(),
			})
		);
	}

	private static void AEndTurn_Begin_Prefix(State s, Combat c)
	{
		if (c.hand.Count == 0)
			return;
		if (s.EnumerateAllArtifacts().OfType<LongTermMemoryArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact is { RetainCooldown: > 0, DiscardCooldown: > 0 })
			return;

		c.QueueImmediate(new Action { artifactPulse = artifact.Key() });
	}

	private sealed class Action : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			if (s.EnumerateAllArtifacts().OfType<LongTermMemoryArtifact>().FirstOrDefault() is not { } artifact)
				return null;

			var customActions = new List<IKokoroApi.IV2.IMultiCardBrowseApi.ICustomAction>(capacity: 2);
			if (artifact.RetainCooldown <= 0)
				customActions.Add(ModEntry.Instance.KokoroApi.MultiCardBrowse.MakeCustomAction(
					new RetainAction(),
					ModEntry.Instance.Localizations.Localize(["artifact", "LongTermMemory", "retainAction"])
				).SetMinSelected(1).SetMaxSelected(1));
			if (artifact.DiscardCooldown <= 0)
				customActions.Add(ModEntry.Instance.KokoroApi.MultiCardBrowse.MakeCustomAction(
					new DiscardAction(),
					ModEntry.Instance.Localizations.Localize(["artifact", "LongTermMemory", "discardAction"])
				).SetMinSelected(1).SetMaxSelected(1));

			if (customActions.Count == 0)
				return null;

			var route = ModEntry.Instance.KokoroApi.MultiCardBrowse.MakeRoute(new CardBrowse
			{
				browseSource = CardBrowse.Source.Hand,
				browseAction = new TitleAction(),
				allowCancel = true,
			})
				.SetEnabledSorting(false)
				.SetBrowseActionIsOnlyForTitle(true)
				.SetCustomActions(customActions)
				.AsRoute;
			
			c.Queue(new ADelay
			{
				time = 0.0,
				timer = 0.0
			});
			if (route.GetCardList(g).Count == 0)
			{
				timer = 0.0;
				return null;
			}
			return route;
		}
	}

	private sealed class TitleAction : CardAction
	{
		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "LongTermMemory", "browseText"]);
	}

	private sealed class RetainAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var affectedCards = c.hand
				.Where(handCard => (ModEntry.Instance.KokoroApi.MultiCardBrowse.GetSelectedCards(this) ?? []).Any(card => card.uuid == handCard.uuid))
				.ToList();

			foreach (var card in affectedCards)
			{
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ModEntry.Instance.Helper.Content.Cards.RetainCardTrait, true, permanent: false);
				ModEntry.Instance.Helper.ModData.SetModData(card, "ShouldRemoveRetain", true);
			}

			if (affectedCards.Count != 0)
			{
				Audio.Play(Event.CardHandling);
				if (s.EnumerateAllArtifacts().OfType<LongTermMemoryArtifact>().FirstOrDefault() is { } artifact)
					artifact.RetainCooldown = 2;
			}
		}
	}

	private sealed class DiscardAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var affectedCards = c.hand
				.Where(handCard => (ModEntry.Instance.KokoroApi.MultiCardBrowse.GetSelectedCards(this) ?? []).Any(card => card.uuid == handCard.uuid))
				.ToList();

			c.isPlayerTurn = true;
			for (var i = 0; i < affectedCards.Count; i++)
			{
				var card = affectedCards[i];
				c.hand.Remove(card);
				card.waitBeforeMoving = i * 0.05;
				card.OnDiscard(s, c);
				c.SendCardToDiscard(s, card);
			}
			c.isPlayerTurn = false;

			if (affectedCards.Count != 0)
			{
				Audio.Play(Event.CardHandling);
				if (s.EnumerateAllArtifacts().OfType<LongTermMemoryArtifact>().FirstOrDefault() is { } artifact)
					artifact.DiscardCooldown = 2;
			}
		}
	}
}