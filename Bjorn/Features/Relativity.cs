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

namespace Shockah.Bjorn;

internal sealed class RelativityManager : IRegisterable, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	private static readonly UK MoveEnemyLeftUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK MoveEnemyRightUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	internal static IStatusEntry RelativityStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		RelativityStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Relativity", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Relativity.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Relativity", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Relativity", "description"]).Localize
		});

		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new StatusLogicHook(), 0);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderHook(), 0);
		ModEntry.Instance.KokoroApi.EvadeHook.DefaultAction.RegisterPaymentOption(new MovementPaymentOption(), -50);
		ModEntry.Instance.KokoroApi.DroneShiftHook.DefaultAction.RegisterPaymentOption(new MovementPaymentOption(), -50);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderMoveButtons)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Postfix))
		);
	}

	private static void DoMoveEnemy(G g, Combat combat, int dir)
	{
		if (!combat.PlayerCanAct(g.state))
			return;

		var isDevOverride = FeatureFlags.Debug && Input.shift;
		if (!isDevOverride && g.state.ship.Get(RelativityStatus.Status) <= 0)
			return;

		var lockdowned = combat.otherShip.Get(Status.lockdown) > 0;
		if (!isDevOverride && lockdowned)
		{
			Audio.Play(Event.Status_PowerDown);
			combat.otherShip.shake += 1.0;
			return;
		}

		combat.Queue(new AMove { dir = dir, targetPlayer = false, fromEvade = true });
		if (!isDevOverride)
			g.state.ship.Add(RelativityStatus.Status, -1);
	}

	private static void Combat_RenderMoveButtons_Postfix(Combat __instance, G g)
	{
		if (g.state.ship.Get(RelativityStatus.Status) <= 0)
			return;

		var buttonWidth = 24;
		var buttonHeight = 33;
		var spread = 96;
		var buttonTop = 4;

		var leftResult = SharedArt.ButtonSprite(g, new Rect(Combat.cardCenter.x - spread - 24, buttonTop, buttonWidth, buttonHeight), MoveEnemyLeftUk, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: true, onMouseDown: new MouseDownHandler(() =>
		{
			DoMoveEnemy(g, __instance, -1);
		}));

		var rightResult = SharedArt.ButtonSprite(g, new Rect(Combat.cardCenter.x + spread + 1, buttonTop, buttonWidth, buttonHeight), MoveEnemyRightUk, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: false, onMouseDown: new MouseDownHandler(() =>
		{
			DoMoveEnemy(g, __instance, 1);
		}));

		if (leftResult.isHover || rightResult.isHover)
			g.state.ship.statusEffectPulses[RelativityStatus.Status] = 0.05;
	}

	private sealed class StatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
		{
			if (args.Status != RelativityStatus.Status)
				return args.NewAmount;
			return Math.Min(args.NewAmount, 3);
		}
	}

	private sealed class StatusRenderHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public (IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
		{
			if (args.Status != RelativityStatus.Status)
				return default;

			var colors = new Color[3];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = args.Amount > i ? ModEntry.Instance.KokoroApi.StatusRendering.DefaultActiveStatusBarColor : ModEntry.Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;

			return (Colors: colors, BarSegmentWidth: null);
		}

		public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		{
			if (args.Status != RelativityStatus.Status)
				return args.Tooltips;

			var tooltipList = args.Tooltips.ToList();
			var index = tooltipList.FindIndex(t => t is TTGlossary glossary && glossary.key == $"status.{RelativityStatus.Status}");
			if (index == -1)
				return tooltipList;

			var (moveLeft, moveRight) = new TTGlossary("").GetLeftRightTooltipKeys("status.evade")!.Value;
			var (shiftLeft, shiftRight) = new TTGlossary("").GetLeftRightTooltipKeys("status.droneShift")!.Value;

			tooltipList[index] = new GlossaryTooltip($"status.{RelativityStatus.Status}")
			{
				Icon = RelativityStatus.Configuration.Definition.icon,
				TitleColor = Colors.status,
				Title = ModEntry.Instance.Localizations.Localize(["status", "Relativity", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["status", "Relativity", "description"], new
				{
					MoveLeft = moveLeft,
					MoveRight = moveRight,
					ShiftLeft = shiftLeft,
					ShiftRight = shiftRight,
				}),
			};
			return tooltipList;
		}
	}

	private sealed class MovementPaymentOption : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption, IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption
	{
		public bool CanPayForEvade(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.ICanPayForEvadeArgs args)
			=> args.State.ship.Get(RelativityStatus.Status) > 0;

		public IReadOnlyList<CardAction> ProvideEvadePaymentActions(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IProvideEvadePaymentActionsArgs args)
		{
			args.State.ship.Add(RelativityStatus.Status, -1);
			return [];
		}

		public void EvadeButtonHovered(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IEvadeButtonHoveredArgs args)
			=> args.State.ship.statusEffectPulses[RelativityStatus.Status] = 0.05;

		public bool CanPayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.ICanPayForDroneShiftArgs args)
			=> args.State.ship.Get(RelativityStatus.Status) > 0;

		public IReadOnlyList<CardAction> ProvideDroneShiftPaymentActions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IProvideDroneShiftPaymentActionsArgs args)
		{
			args.State.ship.Add(RelativityStatus.Status, -1);
			return [];
		}

		public void DroneShiftButtonHovered(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IDroneShiftButtonHoveredArgs args)
			=> args.State.ship.statusEffectPulses[RelativityStatus.Status] = 0.05;
	}
}