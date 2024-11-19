using FSPRO;
using HarmonyLib;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class MindMapManager : IKokoroApi.IV2.IStatusRenderingApi.IHook
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

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AEndTurn_Begin_Prefix))
		);

		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
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

	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == MindMapStatus.Status)
			return [.. args.Tooltips, new TTGlossary("cardtrait.retain")];
		return args.Tooltips;
	}

	private sealed class Action : CardAction
	{
		public required int Amount;

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var route = ModEntry.Instance.KokoroApi.MultiCardBrowse.MakeRoute(r =>
			{
				r.browseSource = CardBrowse.Source.Hand;
				r.browseAction = new BrowseAction { Amount = Amount };
				r.filterRetain = false;
			}).SetMaxSelected(Amount).AsRoute;
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
			=> ModEntry.Instance.Localizations.Localize(["status", "MindMap", "browseText"], new { Amount });

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var cards = ModEntry.Instance.KokoroApi.MultiCardBrowse.GetSelectedCards(this) ?? [];
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
