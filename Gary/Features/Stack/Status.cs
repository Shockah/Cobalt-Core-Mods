using System.Collections.Generic;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Gary;

internal partial class Stack : IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry ApmStatus { get; private set; } = null!;
	
	private static void HandleStatus()
	{
		ApmStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("APM", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/APM.png")).Sprite,
				color = new("FAE4BE"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "APM", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "APM", "description"]).Localize
		});

		var instance = new Stack();
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(instance);
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> args.Status == ApmStatus.Status ? [
			.. args.Tooltips,
			MakeStackedMidrowAttributeTooltip(),
			MakeWobblyMidrowAttributeTooltip(),
		] : args.Tooltips;
}