using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class IsaacRiggsArtifact : DuoArtifact
{
	protected internal override void ApplyPatches(IHarmony harmony)
	{
		var paymentOption = new PaymentOption();
		Instance.KokoroApi.EvadeHook.DefaultAction.RegisterPaymentOption(paymentOption, -10);
		Instance.KokoroApi.DroneShiftHook.DefaultAction.RegisterPaymentOption(paymentOption, -10);
	}
	
	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 1)
			combat.QueueImmediate(new AStatus
			{
				status = Status.evade,
				statusAmount = 1,
				targetPlayer = true,
				artifactPulse = Key()
			});
	}

	private sealed class PaymentOption : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption, IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption
	{
		public bool CanPayForEvade(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.ICanPayForEvadeArgs args)
		{
			if (!args.State.EnumerateAllArtifacts().Any(a => a is IsaacRiggsArtifact))
				return false;
			return args.State.ship.Get(Status.droneShift) > 0;
		}

		public IReadOnlyList<CardAction> ProvideEvadePaymentActions(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IProvideEvadePaymentActionsArgs args)
		{
			if (args.State.EnumerateAllArtifacts().OfType<IsaacRiggsArtifact>().FirstOrDefault() is not { } artifact)
				return [];

			args.State.ship.Add(Status.droneShift, -1);
			artifact.Pulse();
			return [];
		}

		public bool CanPayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.ICanPayForDroneShiftArgs args)
		{
			if (!args.State.EnumerateAllArtifacts().Any(a => a is IsaacRiggsArtifact))
				return false;
			return args.State.ship.Get(Status.evade) > 0;
		}

		public IReadOnlyList<CardAction> ProvideDroneShiftPaymentActions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IProvideDroneShiftPaymentActionsArgs args)
		{
			if (args.State.EnumerateAllArtifacts().OfType<IsaacRiggsArtifact>().FirstOrDefault() is not { } artifact)
				return [];

			args.State.ship.Add(Status.evade, -1);
			artifact.Pulse();
			return [];
		}

		public void EvadeButtonHovered(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IEvadeButtonHoveredArgs args)
			=> args.State.ship.statusEffectPulses[Status.droneShift] = 0.05;

		public void DroneShiftButtonHovered(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IDroneShiftButtonHoveredArgs args)
			=> args.State.ship.statusEffectPulses[Status.evade] = 0.05;
	}
}