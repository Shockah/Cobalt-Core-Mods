﻿namespace Shockah.Soggins;

public interface ISogginsApi
{
	int GetMinSmug(Ship ship);
	int GetMaxSmug(Ship ship);
	int? GetSmug(Ship ship);
	void SetSmug(Ship ship, int? value);
	void AddSmug(Ship ship, int value);
	bool IsOversmug(Ship ship);
	double GetSmugBotchChance(Ship ship);
	double GetSmugDoubleChance(Ship ship);

	bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context);
	void RegisterFrogproofHook(IFrogproofHook hook, double priority);
	void UnregisterFrogproofHook(IFrogproofHook hook);
}

public enum FrogproofHookContext
{
	Rendering, Action
}

public interface IFrogproofHook
{
	bool? IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context);
	void PayForFrogproof(State state, Combat? combat, Card card);
}