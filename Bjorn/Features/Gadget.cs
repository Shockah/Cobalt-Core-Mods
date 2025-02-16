using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Dracula;
using Shockah.Kokoro;

namespace Shockah.Bjorn;

internal sealed class GadgetManager : IRegisterable
{
	private const int MaxStacks = 15;
	
	internal static IStatusEntry GadgetStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		GadgetStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Gadget", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Gadget.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
				affectedByTimestop = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Gadget", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Gadget", "description"]).Localize
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			var stacks = state.ship.Get(GadgetStatus.Status);
			
			if (stacks >= MaxStacks)
			{
				state.rewardsQueue.QueueImmediate(new AArtifactOffering
				{
					amount = 2,
					limitPools = [ArtifactPool.Common],
				});
				
				ModEntry.Instance.Helper.ModData.RemoveModData(state, "GadgetProgress");
			}
			else if (stacks > 0)
			{
				ModEntry.Instance.Helper.ModData.SetModData(state, "GadgetProgress", stacks);
			}
			else
			{
				ModEntry.Instance.Helper.ModData.RemoveModData(state, "GadgetProgress");
			}
		});

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatStart), (State state) =>
		{
			state.ship.Set(GadgetStatus.Status, ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(state, "GadgetProgress"));
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Postfix))
		);
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
		
		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api => api.RegisterBloodTapOptionProvider(GadgetStatus.Status, (_, _, status) => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
			])
		);
	}

	private static void State_PopulateRun_Postfix(State __instance)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "GadgetProgress");

	private sealed class StatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		{
			if (args.Status != GadgetStatus.Status)
				return null;
			
			var colors = new Color[MaxStacks];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = args.Amount > i ? ModEntry.Instance.KokoroApi.StatusRendering.DefaultActiveStatusBarColor : ModEntry.Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;

			return ModEntry.Instance.KokoroApi.StatusRendering.MakeBarStatusInfoRenderer().SetSegments(colors).SetRows(3);
		}
	}
}