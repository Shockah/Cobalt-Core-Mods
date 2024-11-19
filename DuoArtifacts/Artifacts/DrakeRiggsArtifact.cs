using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Kokoro;
using Shockah.Shared;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeRiggsArtifact : DuoArtifact, IKokoroApi.IV2.IEvadeHookApi.IHook, IKokoroApi.IV2.IHookPriority
{
	internal static ExternalSprite InactiveSprite { get; private set; } = null!;

	public bool UsedThisTurn;

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

	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
	{
		if (UsedThisTurn)
			return null;
		return true;
	}

	public void PayForEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs args)
	{
		var artifact = args.State.EnumerateAllArtifacts().OfType<DrakeRiggsArtifact>().First();
		artifact.Pulse();
		artifact.UsedThisTurn = true;
		args.Combat.QueueImmediate(new AStatus
		{
			status = Status.heat,
			statusAmount = 1,
			targetPlayer = true
		});
	}
}