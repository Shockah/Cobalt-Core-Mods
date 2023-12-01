namespace Shockah.Rerolls;

public record CardOfferingConfig(
	int Count,
	Deck? LimitDeck,
	BattleType BattleType,
	Rarity? RarityOverride,
	bool? OverrideUpgradeChances,
	bool MakeAllCardsTemporary,
	bool InCombat,
	int Discount
);
