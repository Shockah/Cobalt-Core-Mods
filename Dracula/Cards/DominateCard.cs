using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Dracula;

internal sealed class DominateCard : Card, IDraculaCard
{
	private static readonly UK MidrowExecutionUK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK CancelExecutionUK = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	private static ISpriteEntry NonFlipArt = null!;
	private static ISpriteEntry OptionalOnIcon = null!;
	private static ISpriteEntry OptionalOffIcon = null!;

	[JsonProperty]
	private bool DisabledFlip;

	[JsonProperty]
	private bool DisabledSecondaryAction;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		NonFlipArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DominateNonFlip.png"));
		OptionalOnIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/OptionalOn.png"));
		OptionalOffIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/OptionalOff.png"));

		helper.Content.Cards.RegisterCard("Dominate", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Dominate.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dominate", "name"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetAllTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.IsVisible)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_IsVisible_Postfix))
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = DisabledFlip ? NonFlipArt.Sprite : null,
			cost = 1,
			floppable = true,
			description = string.Join(" ", [
				ModEntry.Instance.Localizations.Localize(["card", "Dominate", "description", "Flip", DisabledFlip ? "inactive" : "active"]),
				.. (upgrade == Upgrade.A ? [ModEntry.Instance.Localizations.Localize(["card", "Dominate", "description", "Bubble", DisabledSecondaryAction ? "inactive" : "active"])] : Array.Empty<string>()),
				.. (upgrade == Upgrade.B ? [ModEntry.Instance.Localizations.Localize(["card", "Dominate", "description", "Trigger", DisabledSecondaryAction ? "inactive" : "active"])] : Array.Empty<string>()),
				ModEntry.Instance.Localizations.Localize(["card", "Dominate", "description", "Draw"]),
			]),
		};

	public override void OnFlip(G g)
	{
		base.OnFlip(g);

		var sum = (DisabledFlip ? 0 : 1) + (upgrade == Upgrade.None || DisabledSecondaryAction ? 0 : 2);
		var maxSum = upgrade == Upgrade.None ? 1 : 3;
		var newSum = sum - 1;
		if (newSum < 0)
			newSum += maxSum + 1;

		DisabledFlip = (newSum & 1) == 0;
		DisabledSecondaryAction = (newSum & 2) == 0;
		flipped = DisabledFlip || (upgrade != Upgrade.None && DisabledSecondaryAction);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new Action { Flip = !DisabledFlip, Bubble = !DisabledSecondaryAction },
				new ADrawCard { count = 1 },
			],
			Upgrade.B => [
				new Action { Flip = !DisabledFlip, Trigger = !DisabledSecondaryAction },
				new ADrawCard { count = 1 },
			],
			_ => [
				new Action { Flip = !DisabledFlip },
				new ADrawCard { count = 1 },
			]
		};

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("floppable")
				)
				.Find(ILMatches.LdcI4((int)StableSpr.icons_floppable))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ReplaceFloppableIcon)))
				)
				.Find(ILMatches.Ldfld("flipped"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ReplaceFlipped)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static Spr Card_Render_Transpiler_ReplaceFloppableIcon(Spr sprite, Card card)
	{
		if (card is not DominateCard)
			return sprite;
		return (card.flipped ? OptionalOffIcon : OptionalOnIcon).Sprite;
	}

	private static bool Card_Render_Transpiler_ReplaceFlipped(bool flipped, Card card)
		=> card is not DominateCard && flipped;

	private static void Card_GetAllTooltips_Postfix(Card __instance, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (__instance is not DominateCard)
			return;

		__result = __result
			.Select(tooltip =>
			{
				if (tooltip is not TTGlossary { key: "cardtrait.floppable" })
					return tooltip;

				var buttonText = PlatformIcons.GetPlatform() switch
				{
					Platform.NX => Loc.T("controller.nx.b"),
					Platform.PS => Loc.T("controller.ps.circle"),
					_ => Loc.T("controller.xbox.b"),
				};

				return new GlossaryTooltip("cardtrait.optional")
				{
					Icon = OptionalOffIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "optional", "name"]),
					Description = ModEntry.Instance.Localizations.Localize([
						"cardTrait",
						"optional",
						"description",
						PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"
					], new { Button = buttonText })
				};
			});
	}

	private static void Combat_IsVisible_Postfix(Combat __instance, ref bool __result)
	{
		if (__instance.routeOverride is ActionRoute)
			__result = true;
	}

	private sealed class Action : CardAction
	{
		public bool Flip = true;
		public bool Bubble;
		public bool Trigger;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. (Bubble ? [new TTGlossary("midrow.bubbleShield")] : Array.Empty<Tooltip>())
			];

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			if (!Flip && !Bubble && !Trigger)
			{
				timer = 0;
				return null;
			}
			return new ActionRoute { Flip = Flip, Bubble = Bubble, Trigger = Trigger };
		}
	}

	private sealed class ActionRoute : Route
	{
		public required bool Flip;
		public required bool Bubble;
		public required bool Trigger;

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

			var centerX = g.state.ship.x + g.state.ship.parts.Count / 2.0;
			foreach (var (worldX, @object) in combat.stuff)
			{
				if (Math.Abs(worldX - centerX) > 10)
					continue;
				if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.midrow && key.v == worldX) is not { } realBox)
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
				ModEntry.Instance.Localizations.Localize(["card", "Dominate", "ui", "cancel"]),
				onMouseDown: new MouseDownHandler(() => g.CloseRoute(this))
			);
		}

		private void OnMidrowSelected(G g, StuffBase @object)
		{
			if (g.state.route is not Combat combat)
			{
				g.CloseRoute(this);
				return;
			}

			List<CardAction> actions = [];

			if (Flip)
				actions.Add(new APositionalDroneFlip { WorldX = @object.x });
			if (Bubble && !@object.bubbleShield)
				actions.Add(new APositionalDroneBubble { WorldX = @object.x });
			if (Trigger)
				actions.Add(new APositionalDroneTrigger { WorldX = @object.x });
			if (Bubble && @object.bubbleShield)
				actions.Add(new APositionalDroneBubble { WorldX = @object.x });

			combat.QueueImmediate(actions);
			g.CloseRoute(this);
		}
	}
}
