using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;

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
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
	}

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