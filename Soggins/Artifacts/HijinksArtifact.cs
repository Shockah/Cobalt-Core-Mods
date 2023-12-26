using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Boss })]
public sealed class HijinksArtifact : Artifact, IRegisterableArtifact, ISmugHook, IHookPriority
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Hijinks",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Hijinks.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Hijinks",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.HijinksArtifactName.ToUpper(), I18n.HijinksArtifactDescription);
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
					Text = "I connected a few loose wires, we have extra energy now.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch()
				{
					lines = new()
					{
						new CustomSay()
						{
							who = "comp",
							Text = "Wow! I hope nothing bad will happen.",
							loopTag = "neutral"
						},
						new CustomSay()
						{
							who = Deck.hacker.Key(),
							Text = "I have a bad feeling about this.",
							loopTag = "squint"
						},
						new CustomSay()
						{
							who = Deck.dizzy.Key(),
							Text = "All these readings are wrong.",
							loopTag = "neutral"
						}
					}
				}
			}
		};
	}

	public override void OnReceiveArtifact(State state)
		=> state.ship.baseEnergy++;

	public override void OnRemoveArtifact(State state)
		=> state.ship.baseEnergy--;

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Narrative.SpeakBecauseOfAction(GExt.Instance!, combat, $".{Key()}Trigger");
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	public double HookPriority
		=> -100;

	public double ModifySmugBotchChance(State state, Ship ship, Card? card, double chance)
		=> chance * 2;
}
