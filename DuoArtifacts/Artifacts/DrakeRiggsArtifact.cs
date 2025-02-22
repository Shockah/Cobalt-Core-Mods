using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeRiggsArtifact : DuoArtifact
{
	private static ExternalSprite InactiveSprite { get; set; } = null!;

	public bool UsedThisTurn;
	
	protected internal override void ApplyPatches(IHarmony harmony)
	{
		Instance.KokoroApi.EvadeHook.DefaultAction.RegisterPaymentOption(new EvadePaymentOption(), -100);
	}

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
		=> UsedThisTurn = false;

	private sealed class EvadePaymentOption : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption
	{
		public bool CanPayForEvade(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.ICanPayForEvadeArgs args)
		{
			if (args.State.EnumerateAllArtifacts().OfType<DrakeRiggsArtifact>().FirstOrDefault() is not { } artifact)
				return false;
			return !artifact.UsedThisTurn;
		}

		public IReadOnlyList<CardAction> ProvideEvadePaymentActions(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IProvideEvadePaymentActionsArgs args)
		{
			if (args.State.EnumerateAllArtifacts().OfType<DrakeRiggsArtifact>().FirstOrDefault() is not { } artifact)
				return [];
			if (artifact.UsedThisTurn)
				return [];
			
			artifact.UsedThisTurn = true;
			return [
				new AStatus
				{
					status = Status.heat,
					statusAmount = 1,
					targetPlayer = true,
					artifactPulse = artifact.Key(),
				}
			];
		}

		public void EvadeButtonHovered(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IEvadeButtonHoveredArgs args)
			=> args.State.ship.statusEffectPulses[Status.heat] = 0.05;
	}
}