using daisyowl.text;
using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class RemoteExecutionCard : Card, IRegisterable, IHasCustomCardTraits
{
	private static readonly UK MidrowExecutionUK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK CancelExecutionUK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/RemoteExecution.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RemoteExecution", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 5);

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.IsVisible)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_IsVisible_Postfix))
		);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [ModEntry.Instance.KokoroApi.Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 0, infinite = true, description = ModEntry.Instance.Localizations.Localize(["card", "RemoteExecution", "description", upgrade.ToString()]) },
			Upgrade.A => new() { cost = 1, infinite = true, description = ModEntry.Instance.Localizations.Localize(["card", "RemoteExecution", "description", upgrade.ToString()]) },
			_ => new() { cost = 0, description = ModEntry.Instance.Localizations.Localize(["card", "RemoteExecution", "description", upgrade.ToString()]) },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new SelfDiscountAction { CardId = uuid }).AsCardAction,
				new Action()
			],
			_ => [
				new Action()
			]
		};

	private static void Combat_IsVisible_Postfix(Combat __instance, ref bool __result)
	{
		if (__instance.routeOverride is ActionRoute)
			__result = true;
	}

	private sealed class SelfDiscountAction : CardAction
	{
		public required int CardId;

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("cardtrait.discount", "<c=boldPink>1</c>")];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			if (s.FindCard(CardId) is not { } card)
			{
				timer = 0;
				return;
			}

			card.discount--;
			Audio.Play(Event.Status_PowerUp);
		}
	}

	private sealed class Action : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new ActionRoute();
	}

	private sealed class ActionRoute : Route
	{
		public override bool GetShowOverworldPanels()
			=> true;

		public override bool CanBePeeked()
			=> false;

		public override void Render(G g)
		{
			base.Render(g);

			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}

			Draw.Rect(0, 0, MG.inst.PIX_W, MG.inst.PIX_H, Colors.black.fadeAlpha(0.5));

			var keyPrefix = $"{typeof(ModEntry).Namespace!}::{nameof(RemoteExecutionCard)}";
			for (var i = 0; i < combat.otherShip.parts.Count; i++)
			{
				var part = combat.otherShip.parts[i];
				if (part.intent is null)
					continue;

				if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.part && key.v == i && key.str == "combat_ship_enemy") is not { } realBox)
					continue;

				g.Push(rect: new Rect(realBox.rect.x - i * 16 + 1, realBox.rect.y, realBox.rect.w, realBox.rect.h));

				combat.otherShip.RenderPartUI(g, combat, part, i, keyPrefix, isPreview: false);

				if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.part && key.v == i && key.str == keyPrefix) is { } box)
				{
					var partIndex = i;
					box.onMouseDown = new MouseDownHandler(() => OnPartSelected(g, combat.otherShip, partIndex));
					if (box.IsHover())
					{
						if (!Input.gamepadIsActiveInput)
							MouseUtil.DrawGamepadCursor(box);
						part.hilight = true;
					}
				}

				g.Pop();
			}

			var centerX = g.state.ship.x + g.state.ship.parts.Count / 2.0;
			foreach (var (worldX, @object) in combat.stuff)
			{
				if (Math.Abs(worldX - centerX) > 10)
					continue;
				if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.midrow && key.v == worldX) is not { } realBox)
					continue;
				if ((@object.GetActions(g.state, combat)?.Count ?? 0) == 0)
					continue;

				var box = g.Push(new UIKey(MidrowExecutionUK, worldX), realBox.rect, onMouseDown: new MouseDownHandler(() => OnMidrowSelected(g, @object)));
				@object.Render(g, box.rect.xy);
				if (box.rect.x is > 60.0 and < 464.0 && box.IsHover())
				{
					if (!Input.gamepadIsActiveInput)
						MouseUtil.DrawGamepadCursor(box);
					g.tooltips.Add(box.rect.xy + new Vec(16.0, 24.0), @object.GetTooltips());
					@object.hilight = 2;
				}
				g.Pop();
			}

			SharedArt.ButtonText(
				g,
				new Vec(MG.inst.PIX_W - 69, MG.inst.PIX_H - 31),
				CancelExecutionUK,
				ModEntry.Instance.Localizations.Localize(["card", "RemoteExecution", "ui", "cancel"]),
				onMouseDown: new MouseDownHandler(() => g.CloseRoute(this))
			);
		}

		private void OnPartSelected(G g, Ship ship, int partIndex)
		{
			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}

			var queue = new List<CardAction>(combat.cardActions);
			combat.cardActions.Clear();

			var part = ship.parts[partIndex];
			part.intent?.Apply(g.state, combat, ship, partIndex);
			part.intent = null;

			combat.cardActions.AddRange(queue);
			g.CloseRoute(this);
		}

		private void OnMidrowSelected(G g, StuffBase @object)
		{
			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}

			combat.QueueImmediate(@object.GetActions(g.state, combat));
			g.CloseRoute(this);
		}
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not RemoteExecutionCard || args.Card.upgrade != Upgrade.A)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}