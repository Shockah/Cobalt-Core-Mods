using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class Reprogram : IRegisterable
{
	internal static IStatusEntry ReprogrammedStatus { get; private set; } = null!;
	internal static IStatusEntry DeprogrammedStatus { get; private set; } = null!;

	private static ASpawn? SpawnContext { get; set; }

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ReprogrammedStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Reprogrammed", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Reprogrammed.png")).Sprite,
				color = new("E1FFCF"),
				isGood = false,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Reprogrammed", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Reprogrammed", "description"]).Localize
		});
		DeprogrammedStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Deprogrammed", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Deprogrammed.png")).Sprite,
				color = new("E1FFCF"),
				isGood = false,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Deprogrammed", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Deprogrammed", "description"]).Localize
		});

		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.ReplaceSpawnedThing), (State state, Combat combat, StuffBase thing, bool spawnedByPlayer) =>
		{
			var ship = spawnedByPlayer ? state.ship : combat.otherShip;
			
			if (ship.Get(ReprogrammedStatus.Status) > 0)
			{
				if (thing is RepairKit)
					return thing;
				if (SpawnContext is not null)
					ModEntry.Instance.Helper.ModData.SetModData(SpawnContext, "DecrementStatus", ReprogrammedStatus.Status);
				return new RepairKit { fromPlayer = thing.fromPlayer };
			}
			if (ship.Get(DeprogrammedStatus.Status) > 0)
			{
				if (thing is Asteroid)
					return thing;
				if (SpawnContext is not null)
					ModEntry.Instance.Helper.ModData.SetModData(SpawnContext, "DecrementStatus", DeprogrammedStatus.Status);
				return new Asteroid { fromPlayer = thing.fromPlayer };
			}

			return thing;
		}, 0);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix)), priority: Priority.First),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Finalizer))
		);

		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new Hook());
	}

	private static void ASpawn_Begin_Prefix(ASpawn __instance)
		=> SpawnContext = __instance;

	// delayed status decrement - Johanna breaks it otherwise by calling `Artifact.ReplaceSpawnedThing` twice
	private static void ASpawn_Begin_Finalizer(ASpawn __instance, Combat c)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Status>(__instance, "DecrementStatus") is { } statusToDecrement)
			c.QueueImmediate(new AStatus
			{
				targetPlayer = __instance.fromPlayer,
				status = statusToDecrement,
				statusAmount = -1,
			});

		SpawnContext = null;
	}

	private sealed class Hook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		{
			if (args.Status == ReprogrammedStatus.Status)
				return [.. args.Tooltips, .. new RepairKit().GetTooltips()];
			if (args.Status == DeprogrammedStatus.Status)
				return [.. args.Tooltips, .. new Asteroid().GetTooltips()];
			return args.Tooltips;
		}
	}
}