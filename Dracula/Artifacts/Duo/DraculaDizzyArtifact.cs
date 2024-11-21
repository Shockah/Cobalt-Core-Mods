using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaDizzyArtifact : Artifact, IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IHookPriority
{
	internal const int ResultingOxidation = 2;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DraculaDizzy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaDizzy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDizzy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDizzy", "description"], new { ResultingOxidation }).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, Deck.dizzy]);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			..StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1),
			..StatusMeta.GetTooltips(ModEntry.Instance.KokoroApi.OxidationStatus.Status, ResultingOxidation),
			..StatusMeta.GetTooltips(Status.corrode, 1),
		];

	public double HookPriority
		=> 1;

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != ModEntry.Instance.KokoroApi.OxidationStatus.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		var maxTriggers = args.State.EnumerateAllArtifacts().Any(a => a is ThinBloodArtifact) ? 2 : 1;
		var triggers = Math.Min(maxTriggers, args.Ship.Get(ModEntry.Instance.BleedingStatus.Status));
		if (triggers > 0)
		{
			args.Amount += triggers * ResultingOxidation;
			Pulse();
		}
		return false;
	}
}