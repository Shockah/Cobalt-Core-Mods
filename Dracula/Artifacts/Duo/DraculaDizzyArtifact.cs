using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaDizzyArtifact : Artifact, IRegisterable, IStatusLogicHook, IHookPriority
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
		=> StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1)
			.Concat(StatusMeta.GetTooltips(ModEntry.Instance.OxidationStatus.Status, ResultingOxidation))
			.Concat(StatusMeta.GetTooltips(Status.corrode, 1))
			.ToList();

	public double HookPriority
		=> 1;

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != ModEntry.Instance.OxidationStatus.Status)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		var maxTriggers = state.EnumerateAllArtifacts().Any(a => a is ThinBloodArtifact) ? 2 : 1;
		var triggers = Math.Min(maxTriggers, ship.Get(ModEntry.Instance.BleedingStatus.Status));
		if (triggers > 0)
		{
			amount += triggers * ResultingOxidation;
			Pulse();
		}
		return false;
	}
}