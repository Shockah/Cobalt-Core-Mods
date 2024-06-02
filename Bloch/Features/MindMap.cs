using FSPRO;
using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class MindMapManager : IStatusRenderHook
{
	internal static IStatusEntry MindMapStatus { get; private set; } = null!;

	public MindMapManager()
	{
		MindMapStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("MindMap", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/MindMap.png")).Sprite,
				color = new("F82E2E"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "MindMap", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "MindMap", "description"]).Localize
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), RemoveTemporaryRetainTraits, 0);
		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), RemoveTemporaryRetainTraits, 0);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AEndTurn_Begin_Prefix))
		);

		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
	}

	private void RemoveTemporaryRetainTraits(State state)
	{
		foreach (var card in state.GetAllCards())
		{
			if (ModEntry.Instance.Helper.ModData.TryGetModData(card, "ShouldRemoveRetain", out bool shouldRemoveRetain) && shouldRemoveRetain)
			{
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.Helper.Content.Cards.RetainCardTrait, null, permanent: false);
				ModEntry.Instance.Helper.ModData.RemoveModData(card, "ShouldRemoveRetain");
			}
		}
	}

	private static void AEndTurn_Begin_Prefix(State s, Combat c)
	{
		if (c.cardActions.Any(a => a is AEndTurn))
			return;

		var retain = s.ship.Get(MindMapStatus.Status);
		if (retain <= 0)
			return;

		c.QueueImmediate(new Action { Amount = retain });
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status == MindMapStatus.Status)
			return [..tooltips, new TTGlossary("cardtrait.retain")];
		return tooltips;
	}

	private sealed class Action : CardAction
	{
		public required int Amount;

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var route = new MultiCardBrowse()
			{
				mode = CardBrowse.Mode.Browse,
				browseSource = CardBrowse.Source.Hand,
				browseAction = new BrowseAction { Amount = Amount },
				MaxSelected = Amount,
			};
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

	private sealed class BrowseAction : CardAction
	{
		public required int Amount;

		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["status", "Retain", "browseText"], new { Amount });

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var cards = this.GetSelectedCards();
			foreach (var card in cards)
			{
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ModEntry.Instance.Helper.Content.Cards.RetainCardTrait, true, permanent: false);
				ModEntry.Instance.Helper.ModData.SetModData(card, "ShouldRemoveRetain", true);
			}

			if (cards.Count != 0)
				Audio.Play(Event.CardHandling);
		}
	}
}
