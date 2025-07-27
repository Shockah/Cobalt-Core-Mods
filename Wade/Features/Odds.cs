using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Wade;

internal sealed class Odds : IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	public static IStatusEntry OddsStatus { get; private set; } = null!;
	public static IStatusEntry RedTrendStatus { get; private set; } = null!;
	public static IStatusEntry GreenTrendStatus { get; private set; } = null!;
	public static IStatusEntry LuckyDriveStatus { get; private set; } = null!;
	
	public static ISpriteEntry RollIcon { get; private set; } = null!;
	public static ISpriteEntry GreenConditionIcon { get; private set; } = null!;
	public static ISpriteEntry RedConditionIcon { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		OddsStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Odds", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Odds.png")).Sprite,
				color = new Color("F2F2F2"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Odds", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Odds", "description"]).Localize
		});
		
		GreenTrendStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("GreenTrend", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/GreenTrend.png")).Sprite,
				color = new Color("26CF26"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "GreenTrend", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "GreenTrend", "description"]).Localize
		});
		
		RedTrendStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("RedTrend", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/RedTrend.png")).Sprite,
				color = new Color("C92525"),
				isGood = false,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "RedTrend", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "RedTrend", "description"]).Localize
		});
		
		LuckyDriveStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("LuckyDrive", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/LuckyDrive.png")).Sprite,
				color = new Color("26CF26"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "LuckyDrive", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "LuckyDrive", "description"]).Localize
		});

		RollIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/Roll.png"));
		RedConditionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/RedCondition.png"));
		GreenConditionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icon/GreenCondition.png"));
		
		var instance = new Odds();
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(instance);
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.ModifyBaseDamage), (State state, Combat? combat, bool fromPlayer) =>
		{
			if (!fromPlayer && combat is null)
				return 0;

			var ship = fromPlayer ? state.ship : combat!.otherShip;
			return ship.Get(OddsStatus.Status) <= 0 ? 0 : ship.Get(LuckyDriveStatus.Status);
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (State state, Combat combat, Card card) =>
		{
			if (state.ship.Get(LuckyDriveStatus.Status) <= 0)
				return;
			if (card.GetActionsOverridden(state, combat).Any(action => ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(action).Any(action => action is AAttack)))
				combat.Queue(new RollAction());
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_CanBeNegative_Postfix))
		);
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status != OddsStatus.Status)
			return;
		if (args.NewAmount == 0)
			return;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return;
		
		args.Combat.Queue(new RollAction { TargetPlayer = args.Ship.isPlayerShip, IsTurnStart = true });
	}

	public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
	{
		if (args.Status != OddsStatus.Status)
			return args.NewAmount;
		
		var newAmount = Math.Clamp(args.NewAmount, -args.Ship.Get(RedTrendStatus.Status) - 1, args.Ship.Get(GreenTrendStatus.Status) + 1);
		if (newAmount == 0)
			newAmount = -Math.Sign(args.OldAmount);
		return newAmount;
	}

	public IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(IKokoroApi.IV2.IStatusRenderingApi.IHook.IGetExtraStatusesToShowArgs args)
	{
		if (args.Ship.Get(OddsStatus.Status) == 0 && (args.Ship.Get(RedTrendStatus.Status) != 0 || args.Ship.Get(GreenTrendStatus.Status) != 0))
			yield return (OddsStatus.Status, 0);
	}

	public bool? ShouldShowStatus(IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldShowStatusArgs args)
		=> args.Status == RedTrendStatus.Status || args.Status == GreenTrendStatus.Status ? false : null;

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == OddsStatus.Status)
			return args.Tooltips.Select(tooltip =>
			{
				if (tooltip is TTGlossary glossary && glossary.key == $"status.{OddsStatus.Status}")
					return new GlossaryTooltip(glossary.key)
					{
						Icon = OddsStatus.Configuration.Definition.icon,
						TitleColor = Colors.status,
						Title = OddsStatus.Configuration.Name!(DB.currentLocale.locale),
						Description = ModEntry.Instance.Localizations.Localize(["status", "Odds", "description"], new
						{
							Max = (args.Ship?.Get(GreenTrendStatus.Status) ?? 0) + 1,
							Min = -(args.Ship?.Get(RedTrendStatus.Status) ?? 0) - 1,
						}),
					};
				return tooltip;
			}).ToList();

		if (args.Status == RedTrendStatus.Status || args.Status == GreenTrendStatus.Status || args.Status == LuckyDriveStatus.Status)
			return [
				.. args.Tooltips,
				.. StatusMeta.GetTooltips(OddsStatus.Status, 0),
			];

		return args.Tooltips;
	}

	public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		=> args.Status == OddsStatus.Status ? StatusRenderer.Instance : null;
	
	private sealed class StatusRenderer : IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer
	{
		internal static readonly StatusRenderer Instance = new();
		
		private static readonly IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer BarRenderer = ModEntry.Instance.KokoroApi.StatusRendering.MakeBarStatusInfoRenderer();
		
		public int Render(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs args)
		{
			var totalWidth = 0;
			var newArgs = args.CopyToBuilder();
			
			var negativeThreshold = args.Ship.Get(RedTrendStatus.Status) + 1;
			var positiveThreshold = args.Ship.Get(GreenTrendStatus.Status) + 1;
			var odds = args.Amount;

			for (var i = negativeThreshold; i > 0; i--)
			{
				BarRenderer.Segments = [Colors.downside.fadeAlpha(odds == -i ? 1 : 0.4)];
				BarRenderer.SegmentWidth = 2;
				newArgs.Position = new(args.Position.x + totalWidth, args.Position.y);
				totalWidth += BarRenderer.Render(newArgs) - 1;
			}
			for (var i = positiveThreshold; i > 0; i--)
			{
				BarRenderer.Segments = [Colors.heal.fadeAlpha(odds == i ? 1 : 0.4)];
				BarRenderer.SegmentWidth = 2;
				newArgs.Position = new(args.Position.x + totalWidth, args.Position.y);
				totalWidth += BarRenderer.Render(newArgs) - 1;
			}

			totalWidth += 1;
			return totalWidth;
		}
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == OddsStatus.Status)
			__result = true;
	}

	internal sealed class RollAction : CardAction
	{
		public bool TargetPlayer = true;
		public bool IsTurnStart;
		
		public override Icon? GetIcon(State s)
			=> new(RollIcon.Sprite, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Roll")
				{
					Icon = RollIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "Roll", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "Roll", "description"]),
				},
				.. StatusMeta.GetTooltips(OddsStatus.Status, 0),
			];

		public override void Begin(G g, State s, Combat c)
		{
			var ship = TargetPlayer ? s.ship : c.otherShip;
			var negativeThreshold = ship.Get(RedTrendStatus.Status) + 1;
			var positiveThreshold = ship.Get(GreenTrendStatus.Status) + 1;
			var options = negativeThreshold + positiveThreshold;
			
			var odds = s.rngActions.NextInt() % options - negativeThreshold;
			if (odds >= 0)
				odds++;

			ship.Set(OddsStatus.Status, odds);
			// TODO: sound
			// TODO: cool animation
		}
	}
	
	internal sealed class TrendCondition : IKokoroApi.IV2.IConditionalApi.IBoolExpression
	{
		public required bool Positive;
		public bool? OverrideValue;

		public bool GetValue(State state, Combat combat)
			=> OverrideValue ?? (Positive ? state.ship.Get(OddsStatus.Status) > 0 : state.ship.Get(OddsStatus.Status) < 0);

		public string GetTooltipDescription(State state, Combat? combat)
			=> ModEntry.Instance.Localizations.Localize(["condition", Positive ? "Green" : "Red", "description"]);

		public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
		{
			if (!dontRender)
				Draw.Sprite(
					(Positive ? GreenConditionIcon : RedConditionIcon).Sprite,
					position.x,
					position.y,
					color: isDisabled ? Colors.disabledIconTint : Colors.white
				);
			position.x += 8;
		}

		public IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat? combat, Tooltip defaultTooltip, string defaultTooltipDescription)
			=> [
				new GlossaryTooltip($"AConditional::{ModEntry.Instance.Package.Manifest.UniqueName}::{(Positive ? "Green" : "Red")}")
				{
					Icon = (Positive ? GreenConditionIcon : RedConditionIcon).Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["condition", Positive ? "Green" : "Red", "title"]),
					Description = defaultTooltipDescription,
				}
			];
	}
}