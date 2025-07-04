﻿using Nickel;
using System.Collections.Generic;

namespace Shockah.Dyna;

public interface IDynaApi
{
	IDeckEntry DynaDeck { get; }

	IStatusEntry TempNitroStatus { get; }
	IStatusEntry NitroStatus { get; }
	IStatusEntry BastionStatus { get; }

	PDamMod FluxDamageModifier { get; }

	int GetBlastwaveDamage(Card? card, State state, int baseDamage, bool targetPlayer = false, int blastwaveIndex = 0);

	bool IsBlastwave(AAttack attack);
	bool IsStunwave(AAttack attack);
	int? GetBlastwaveDamage(AAttack attack);
	int GetBlastwaveRange(AAttack attack);
	AAttack SetBlastwave(AAttack attack, int? damage, int range = 1, bool isStunwave = false);

	CardAction MakeBlastwaveOnShipAction(bool targetPlayer, int localX, int? damage, int range = 1, bool isStunwave = false);
	CardAction MakeBlastwaveInMidrowAction(bool playerDidIt, int worldX, int? damage, int range = 1, bool isStunwave = false);

	IDynaCharge MakeBurstCharge();
	IDynaCharge MakeConcussionCharge();
	IDynaCharge MakeDemoCharge();
	IDynaCharge MakeFluxCharge();
	IDynaCharge MakeShatterCharge();
	IDynaCharge MakeSwiftCharge();
	CardAction MakeFireChargeAction(IDynaCharge charge, int offset = 0, bool targetPlayer = false);

	void DefaultRenderChargeImplementation(IDynaCharge charge, G g, State state, Combat combat, Ship ship, int worldX, Vec position);

	IDynaCharge? GetStickedCharge(State state, Combat combat, Part part);
	void SetStickedCharge(State state, Combat combat, Part part, IDynaCharge? charge);
	bool TriggerChargeIfAny(State state, Combat combat, Part part, bool targetPlayer);

	void RegisterHook(IDynaHook hook, double priority);
	void UnregisterHook(IDynaHook hook);
}

public interface IDynaCharge
{
	string Key();
	double YOffset { get; set; }
	int BonkDamage { get => 2; }

	Spr GetIcon(State state);
	Spr? GetLightsIcon(State state) => null;
	void Render(G g, State state, Combat combat, Ship ship, int worldX, Vec position) => ModEntry.Instance.Api.DefaultRenderChargeImplementation(this, g, state, combat, ship, worldX, position);

	IEnumerable<Tooltip> GetTooltips(State state) => [];
	void OnTrigger(State state, Combat combat, Ship ship, Part part) { }
	void OnHitMidrow(State state, Combat combat, bool fromPlayer, int worldX) { }
}

public interface IDynaHook
{
	void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX, bool hitMidrow) { }
	void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX, bool hitMidrow, int? damage, int range, bool isStunwave) => OnBlastwaveTrigger(state, combat, ship, worldX, hitMidrow);
	void OnBlastwaveHit(State state, Combat combat, Ship ship, int originWorldX, int waveWorldX, bool hitMidrow) { }
	void OnBlastwaveHit(State state, Combat combat, Ship ship, int originWorldX, int waveWorldX, bool hitMidrow, int? damage, bool isStunwave) => OnBlastwaveHit(state, combat, ship, originWorldX, waveWorldX, hitMidrow);
	int ModifyBlastwaveDamage(Card? card, State state, bool targetPlayer, int blastwaveIndex) => 0;
	bool ModifyShipBlastwave(State state, Combat combat, AAttack? source, bool targetPlayer, int localX, ref int? damage, ref int range, ref bool isStunwave) => false;
	bool ModifyMidrowBlastwave(State state, Combat combat, AAttack? source, bool playerDidIt, int worldX, ref int? damage, ref int range, ref bool isStunwave) => false;

	void OnChargeFired(State state, Combat combat, Ship targetShip, int worldX) { }
	void OnChargeSticked(State state, Combat combat, Ship ship, int worldX) { }
	void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX) { }
}