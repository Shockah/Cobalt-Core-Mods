using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.Bjorn;

internal sealed class GadgetManager : IRegisterable
{
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
			if (state.ship.Get(GadgetStatus.Status) <= 0)
				return;
			
			state.rewardsQueue.QueueImmediate(new AArtifactOffering
			{
				amount = 2,
				limitPools = [ArtifactPool.Common],
			});
		});
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
	}

	private sealed class StatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public (IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
		{
			if (args.Status != GadgetStatus.Status)
				return null;
			return ([new(0, 0, 0, 0)], -3);
		}
	}
}