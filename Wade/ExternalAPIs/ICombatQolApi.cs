using System;

namespace TheJazMaster.CombatQoL;

public interface ICombatQolApi {
	[Flags] // Ordered from most important to least important (in terms of how much info they reveal)
	public enum InvalidationReason {
		NONE = 0,
		HIDDEN_INFORMATION = 1<<0,
		PILE_CONTENTS = 1<<1,
		UNKNOWN_ORDER = 1<<2,
		RNG_SEED = 1<<3,
		SECRET_BRITTLE = 1<<4,
		DYING_ENEMY = 1<<5,
		DYING_PLAYER = 1<<6,
		TURN_END = 1<<7,
	}
	
	bool IsSimulating();

	void InvalidateUndos(Combat c, InvalidationReason reason);
}