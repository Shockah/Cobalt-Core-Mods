using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Boss])]
public sealed class RepeatedMistakesArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.RepeatedMistakes",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "RepeatedMistakes.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.RepeatedMistakes",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.RepeatedMistakesArtifactName.ToUpper(), I18n.RepeatedMistakesArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void InjectDialogue()
	{
		DB.story.all[$"Artifact{Key()}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			lookup = [$"{Key()}Trigger"],
			allPresent = [Instance.SogginsDeck.GlobalName],
			hasArtifacts = [Key()],
			lines = [
				new CustomSay
				{
					who = Instance.SogginsDeck.GlobalName,
					Text = "Oh no! I made another mistake",
					loopTag = Instance.SmugPortraitAnimations[-2].Tag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.hacker.Key(),
							Text = "...",
							loopTag = "mad"
						},
						new CustomSay
						{
							who = Deck.goat.Key(),
							Text = "How will I launch drones now?",
							loopTag = "squint"
						}
					]
				}
			]
		};
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AStatus
		{
			status = Status.backwardsMissiles,
			statusAmount = 2,
			targetPlayer = true,
			artifactPulse = Key(),
			dialogueSelector = $".{Key()}Trigger"
		});
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		combat.QueueImmediate(new ASpawn
		{
			thing = new Missile { yAnimation = 0.0, missileType = MissileType.seeker },
			artifactPulse = Key(),
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("status.backwardsMissiles"),
			new TTGlossary("action.spawn"),
			new TTGlossary("midrow.missile_seeker", 2),
		];
}
