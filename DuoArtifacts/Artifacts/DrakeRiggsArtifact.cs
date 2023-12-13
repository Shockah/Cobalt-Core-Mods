using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeRiggsArtifact : DuoArtifact, IEvadeHook, IHookPriority
{
	internal static ExternalSprite InactiveSprite { get; private set; } = null!;

	public bool UsedThisTurn = false;

	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterArt(registry, namePrefix, definition);
		InactiveSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}.Inactive",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifacts", "DrakeRiggsInactive.png"))
		);
	}

	public override Spr GetSprite()
		=> UsedThisTurn ? (Spr)InactiveSprite.Id!.Value : base.GetSprite();

	public override void OnTurnStart(State state, Combat combat)
	{
		UsedThisTurn = false;
	}

	public double HookPriority
		=> -10;

	bool? IEvadeHook.IsEvadePossible(State state, Combat combat, EvadeHookContext context)
	{
		if (UsedThisTurn)
			return null;
		return true;
	}

	void IEvadeHook.PayForEvade(State state, Combat combat, int direction)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<DrakeRiggsArtifact>().First();
		artifact.Pulse();
		artifact.UsedThisTurn = true;
		combat.QueueImmediate(new AStatus
		{
			status = Status.heat,
			statusAmount = 1,
			targetPlayer = true
		});
	}
}