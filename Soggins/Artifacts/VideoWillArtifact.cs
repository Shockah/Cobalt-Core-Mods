using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class VideoWillArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.VideoWill",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "VideoWill.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.VideoWill",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.VideoWillArtifactName.ToUpper(), I18n.VideoWillArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void InjectDialogue()
	{
		DB.story.all[$"Artifact{Key()}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			lookup = new() { $"{Key()}Trigger" },
			allPresent = new() { Instance.SogginsDeck.GlobalName },
			hasArtifacts = new() { Key() },
			lines = new()
			{
				new CustomSay()
				{
					who = Instance.SogginsDeck.GlobalName,
					Text = "I look so handsome in this recording.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch()
				{
					lines = new()
					{
						new CustomSay()
						{
							who = Deck.riggs.Key(),
							Text = "Handsome?",
							loopTag = "neutral"
						},
						new CustomSay()
						{
							who = Deck.shard.Key(),
							Text = "Is it your prince form?",
							loopTag = "stoked"
						}
					}
				}
			}
		};
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AStatus
		{
			status = (Status)Instance.FrogproofingStatus.Id!.Value,
			statusAmount = 3,
			targetPlayer = true,
			artifactPulse = Key(),
			dialogueSelector = $".{Key()}Trigger"
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.FrogproofingTooltip);
		return tooltips;
	}
}
