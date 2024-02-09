using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(unremovable = true)]
public sealed class PiratedShipCadArtifact : Artifact, IRegisterableArtifact, ISmugHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.PiratedShipCad",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "PiratedShipCad.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.PiratedShipCad",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.PiratedShipCadArtifactName.ToUpper(), I18n.PiratedShipCadArtifactDescription);
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
				new CustomSay()
				{
					who = Instance.SogginsDeck.GlobalName,
					Text = "I downloaded this from the interwebs.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch()
				{
					lines = [
						new CustomSay()
						{
							who = "comp",
							Text = "Put that thing away from me.",
							loopTag = "squint"
						},
						new CustomSay()
						{
							who = Deck.hacker.Key(),
							Text = "That's a malware risk.",
							loopTag = "mad"
						}
					]
				}
			]
		};
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(Instance.Api.GetSmugTooltip());
		tooltips.Add(new TTGlossary("status.tempShieldAlt"));
		return tooltips;
	}

	public void OnCardBotchedBySmug(State state, Combat combat, Card card)
	{
		combat.Queue(new AStatus
		{
			status = Status.tempShield,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = Key(),
			dialogueSelector = $".{Key()}Trigger"
		});
	}
}
