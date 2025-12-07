using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Shockah.Shared;

namespace Shockah.Johnson;

internal sealed class CouponArtifact : Artifact, IRegisterable
{
	internal static IArtifactEntry Entry { get; private set; } = null!;

	[JsonProperty]
	private int? PreselectedCardId;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("Coupon", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Coupon.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Coupon", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Coupon", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CockpitAnimation), nameof(CockpitAnimation.RenderOverworldInfoPanel)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CockpitAnimation_RenderOverworldInfoPanel_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Render_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
	{
		List<Tooltip> tooltips = [
			new TTGlossary("cardtrait.discount", 1),
		];

		if (MG.inst.g?.state is { } state && !state.IsOutsideRun())
		{
			tooltips.Add(new TTDivider());
			tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(
				["artifact", "Coupon", "preselectHint", PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"], 
				new
				{
					Button = PlatformIcons.GetPlatform() switch
					{
						Platform.NX => Loc.T("controller.nx.a"),
						Platform.PS => Loc.T("controller.ps.x"),
						_ => Loc.T("controller.xbox.a"),
					},
				}
			)));

			if (PreselectedCardId is not null && state.FindCard(PreselectedCardId.Value) is { } card)
			{
				tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "Coupon", "preselectedCard"])));
				tooltips.Add(new TTCard { card = card.CopyWithNewId(), showCardTraitTooltips = false });
			}
		}
		
		return tooltips;
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn || combat.turn != 1)
			return;

		if (PreselectedCardId is null || state.FindCard(PreselectedCardId.Value) is null)
		{
			combat.Queue([
				new ADelay(),
				new ACardSelect
				{
					browseAction = new BrowseAction { Amount = -1 },
					browseSource = CardBrowse.Source.Deck,
					artifactPulse = Key(),
				},
			]);
		}
		else
		{
			combat.Queue(new DiscountPreselectedAction { PreselectedCardId = PreselectedCardId.Value, Amount = -1 });
		}
	}

	private static void MakeArtifactClickable(G g)
	{
		if (g.state.EnumerateAllArtifacts().OfType<CouponArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (g.boxes.LastOrDefault(box => box.key?.k == StableUK.artifact && box.key?.str == artifact.Key()) is not { } box)
			return;
		if ((g.state.route is Combat combat ? combat.routeOverride : g.state.routeOverride) is not null)
			return;

		box.onMouseDown = new MouseDownHandler(() =>
		{
			artifact.PreselectedCardId = null;
			
			var route = new CardBrowse
			{
				browseAction = new PreselectAction(),
				browseSource = CardBrowse.Source.Deck,
				allowCancel = true,
			};

			if (g.state.route is Combat combat)
				combat.routeOverride = route;
			else
				g.state.routeOverride = route;
		});
	}

	private static void CockpitAnimation_RenderOverworldInfoPanel_Postfix(G g)
		=> MakeArtifactClickable(g);

	private static void Combat_Render_Postfix(G g)
		=> MakeArtifactClickable(g);

	private sealed class DiscountPreselectedAction : CardAction
	{
		public required int PreselectedCardId;
		public required int Amount;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (s.FindCard(PreselectedCardId) is not { } card)
			{
				timer = 0;
				return;
			}
			
			card.discount += Amount;
		}
	}
	
	private sealed class BrowseAction : CardAction
	{
		public required int Amount;

		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "Coupon", "browseAction"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			selectedCard.discount += Amount;
		}
	}
	
	private sealed class PreselectAction : CardAction
	{
		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "Coupon", "preselectAction"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (g.state.EnumerateAllArtifacts().OfType<CouponArtifact>().FirstOrDefault() is not { } artifact)
			{
				timer = 0;
				return;
			}

			artifact.PreselectedCardId = selectedCard?.uuid;
		}
	}
}
