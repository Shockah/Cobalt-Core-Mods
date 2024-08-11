using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

internal sealed class Relativity : IRegisterable, IStatusRenderHook
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

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(new StatusLogicHook(), 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(new StatusRenderHook(), 0);
		ModEntry.Instance.KokoroApi.RegisterEvadeHook(new MovementHook(), -50);
		ModEntry.Instance.KokoroApi.RegisterDroneShiftHook(new MovementHook(), -50);

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

		SharedArt.ButtonSprite(g, new Rect(Combat.cardCenter.x - spread - 24, buttonTop, buttonWidth, buttonHeight), MoveEnemyLeftUk, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: true, onMouseDown: new MouseDownHandler(() =>
		{
			DoMoveEnemy(g, __instance, -1);
		}));

		SharedArt.ButtonSprite(g, new Rect(Combat.cardCenter.x + spread + 1, buttonTop, buttonWidth, buttonHeight), MoveEnemyRightUk, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: false, onMouseDown: new MouseDownHandler(() =>
		{
			DoMoveEnemy(g, __instance, 1);
		}));
	}

	private sealed class StatusLogicHook : IStatusLogicHook
	{
		public int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount)
		{
			if (status != RelativityStatus.Status)
				return newAmount;
			return Math.Min(newAmount, 3);
		}
	}

	private sealed class StatusRenderHook : IStatusRenderHook
	{
		public bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount)
			=> status == RelativityStatus.Status ? true : null;

		public (IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount)
		{
			if (status != RelativityStatus.Status)
				return default;

			var colors = new Color[3];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = amount > i ? ModEntry.Instance.KokoroApi.DefaultActiveStatusBarColor : ModEntry.Instance.KokoroApi.DefaultInactiveStatusBarColor;

			return (Colors: colors, BarTickWidth: null);
		}

		public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
		{
			if (status != RelativityStatus.Status)
				return tooltips;

			var index = tooltips.FindIndex(t => t is TTGlossary glossary && glossary.key == $"status.{RelativityStatus.Status}");
			if (index == -1)
				return tooltips;

			var (moveLeft, moveRight) = new TTGlossary("").GetLeftRightTooltipKeys("status.evade")!.Value;
			var (shiftLeft, shiftRight) = new TTGlossary("").GetLeftRightTooltipKeys("status.droneShift")!.Value;

			tooltips[index] = new GlossaryTooltip($"status.{RelativityStatus.Status}")
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
			return tooltips;
		}
	}

	private sealed class MovementHook : IEvadeHook, IDroneShiftHook
	{
		public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
			=> state.ship.Get(RelativityStatus.Status) > 0 ? true : null;

		public void PayForEvade(State state, Combat combat, int direction)
			=> state.ship.Add(RelativityStatus.Status, -1);

		public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
			=> state.ship.Get(RelativityStatus.Status) > 0 ? true : null;

		public void PayForDroneShift(State state, Combat combat, int direction)
			=> state.ship.Add(RelativityStatus.Status, -1);
	}
}