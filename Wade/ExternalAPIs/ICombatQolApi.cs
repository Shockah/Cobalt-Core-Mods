using System;

namespace TheJazMaster.CombatQoL;

public interface ICombatQolApi {
	[Flags] // Ordered from most important to least important (in terms of how much info they reveal)
	public enum InvalidationReason {
		NONE = 0,
		PILE_CONTENTS = 1<<0,
		UNKNOWN_ORDER = 1<<1,
		RNG_SEED = 1<<2,
		SECRET_BRITTLE = 1<<3,
		DYING_ENEMY = 1<<4,
		DYING_PLAYER = 1<<5,
		TURN_END = 1<<6,
	}
	enum RngTypes {
		ACTION, SHUFFLE, AI, CARD_OFFERINGS, CARD_OFFERINGS_MIDCOMBAT, ARTIFACT_OFFERINGS
	}

	void MarkSafeRngAdvance(Combat c, RngTypes type, int count = 1);

	void ClearKnownCards(Combat c, CardDestination destination);

	void InvalidateUndos(Combat c, InvalidationReason reason);

	void MarkCardAsOkayToBeGone(Combat c, Card card);

	void MarkCardAsOkayToBeGone(Combat c, int uuid);
}