using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class HotTubArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.HotTub",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "HotTub.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.HotTub",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.HotTubArtifactName.ToUpper(), I18n.HotTubArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetLocDesc)),
			postfix: new HarmonyMethod(GetType(), nameof(Artifact_GetLocDesc_Postfix))
		);
	}

	public void InjectDialogue()
	{
		DB.story.all[$"Artifact{Key()}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			allPresent = [Instance.SogginsDeck.GlobalName],
			hasArtifacts = [Key()],
			turnStart = true,
			maxTurnsThisCombat = 1,
			lines = [
				new CustomSay()
				{
					who = Instance.SogginsDeck.GlobalName,
					Text = "The bridge is the best place to install one of these.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch()
				{
					lines = [
						new CustomSay()
						{
							who = Deck.eunice.Key(),
							Text = "I can make it much hotter if you want.",
							loopTag = "sly"
						}
					]
				}
			]
		};
	}

	public override string Description()
	{
		if (MG.inst.g.state is not { } state)
			return base.Description();
		return string.Format(I18n.HotTubArtifactDescription, Instance.Api.GetMinSmug(state.ship), Instance.Api.GetMaxSmug(state.ship));
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.turn == 0)
			return;

		var smug = Instance.Api.GetSmug(state, state.ship);
		if (smug is null)
			return;
		if (Instance.Api.IsOversmug(state, state.ship))
			return;

		int sign = Math.Sign(smug.Value - 0);
		if (sign == 0)
			return;

		combat.Queue(new AStatus
		{
			status = (Status)Instance.Api.SmugStatus.Id!.Value,
			statusAmount = -sign,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	private static void Artifact_GetLocDesc_Postfix(Artifact __instance, ref string __result)
	{
		if (__instance is not HotTubArtifact)
			return;
		__result = __instance.Description();
	}
}
