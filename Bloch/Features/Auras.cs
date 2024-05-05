using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class AuraManager : IStatusLogicHook, IStatusRenderHook
{
	internal static IStatusEntry VeilingStatus { get; private set; } = null!;
	internal static IStatusEntry FeedbackStatus { get; private set; } = null!;
	internal static IStatusEntry InsightStatus { get; private set; } = null!;
	internal static IStatusEntry IntensifyStatus { get; private set; } = null!;

	private static bool IsDuringNormalDamage = false;
	private static int ReducedDamage = 0;

	public AuraManager()
	{
		VeilingStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Veiling", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Veiling.png")).Sprite,
				color = new("A17FFF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Veiling", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Veiling", "description"]).Localize
		});

		FeedbackStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Feedback", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Feedback.png")).Sprite,
				color = new("A17FFF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Feedback", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Feedback", "description"]).Localize
		});

		InsightStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Insight", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Insight.png")).Sprite,
				color = new("A17FFF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Insight", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Insight", "description"]).Localize
		});

		IntensifyStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Intensify", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Intensify.png")).Sprite,
				color = new("DBC6FF"),
				affectedByTimestop = true,
				isGood = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Intensify", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Intensify", "description"]).Localize
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnQueueEmptyDuringPlayerTurn), (Combat combat) =>
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(combat, "TriggeredInsight");
		}, 0);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatStart), (Combat combat) =>
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(combat, "TriggeredInsight");
		}, 0);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnEnd), (Combat combat) =>
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(combat, "TriggeredInsight");
		}, 0);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ModifyDamageDueToParts)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_ModifyDamageDueToParts_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrawCards)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_DrawCards_Prefix))
		);

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
	}

	private static void Ship_NormalDamage_Prefix()
		=> IsDuringNormalDamage = true;

	private static void Ship_NormalDamage_Finalizer(Ship __instance, Combat c, ref DamageDone __result)
	{
		IsDuringNormalDamage = false;

		var feedback = __instance.Get(FeedbackStatus.Status);
		var maxFeedback = Math.Min(feedback, __instance.Get(IntensifyStatus.Status) + 1);

		if (maxFeedback > 0)
			c.QueueImmediate([
				new AStatus
				{
					targetPlayer = true,
					status = FeedbackStatus.Status,
					statusAmount = -maxFeedback,
					statusPulse = maxFeedback > 1 ? IntensifyStatus.Status : null
				},
				new AHurt
				{
					targetPlayer = !__instance.isPlayerShip,
					hurtAmount = maxFeedback,
					statusPulse = FeedbackStatus.Status
				}
			]);

		if (ReducedDamage <= 0)
			return;

		c.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = VeilingStatus.Status,
			statusAmount = -ReducedDamage,
			statusPulse = ReducedDamage > 1 ? IntensifyStatus.Status : null
		});
		ReducedDamage = 0;
	}

	private static void Ship_ModifyDamageDueToParts_Prefix(Ship __instance, ref int incomingDamage, Part part, bool piercing)
	{
		if (piercing)
			return;
		if (!IsDuringNormalDamage)
			return;
		if (__instance.Get(Status.perfectShield) > 0)
			return;

		var modifier = part.GetDamageModifier();
		if (incomingDamage <= (modifier == PDamMod.weak ? -1 : 0))
			return;

		var veiling = __instance.Get(VeilingStatus.Status);
		var maxVeiling = Math.Min(veiling, __instance.Get(IntensifyStatus.Status) + 1);
		if (maxVeiling <= 0)
			return;

		var toReduce = Math.Min(maxVeiling, incomingDamage - (modifier == PDamMod.armor ? 1 : 0) + (modifier == PDamMod.weak ? 1 : 0));
		if (toReduce <= 0)
			return;

		incomingDamage -= toReduce;
		ReducedDamage = toReduce;
	}

	private static bool Combat_DrawCards_Prefix(Combat __instance, State s, int count)
	{
		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "TriggeredInsight"))
			return true;

		var insight = s.ship.Get(InsightStatus.Status);
		var maxInsight = Math.Min(Math.Min(insight, s.ship.Get(IntensifyStatus.Status) + 1), s.deck.Count + __instance.discard.Count);

		if (maxInsight <= 0)
			return true;

		ModEntry.Instance.Helper.ModData.SetModData(__instance, "TriggeredInsight", true);
		__instance.QueueImmediate([
			new AStatus
			{
				targetPlayer = true,
				status = InsightStatus.Status,
				statusAmount = -maxInsight,
				statusPulse = maxInsight > 1 ? IntensifyStatus.Status : null,
			},
			new ScryAction
			{
				Amount = maxInsight,
				FromInsight = true,
				statusPulse = InsightStatus.Status,
			},
			new ADrawCard { count = count }
		]);
		return false;
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != IntensifyStatus.Status)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return false;
		if (amount == 0)
			return false;

		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is ComposureArtifact) is { } composureArtifact)
		{
			composureArtifact.Pulse();
			amount = Math.Max(amount - 1, 0);
		}
		else
		{
			amount = 0;
		}
		return false;
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status == InsightStatus.Status)
			return [..tooltips, ..new ScryAction { Amount = 1 }.GetTooltips(DB.fakeState)];
		return tooltips;
	}
}
