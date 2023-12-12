using CobaltCoreModding.Definitions.ExternalItems;
using System;

namespace Shockah.Soggins;

public sealed class ApiImplementation : ISogginsApi
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly double[] BotchChances = new double[] { 0.15, 0.14, 0.12, 0.10, 0.08, 0.06, 0.05 };
	private static readonly double[] DoubleChances = new double[] { 0.05, 0.06, 0.08, 0.10, 0.12, 0.14, 0.15 };

	public Tooltip FrogproofCardTraitTooltip
		=> new CustomTTGlossary(CustomTTGlossary.GlossaryType.cardtrait, () => (Spr)Instance.FrogproofSprite.Id!.Value, () => I18n.FrogproofCardTraitName, () => I18n.FrogproofCardTraitText);

	public ExternalStatus FrogproofingStatus
		=> Instance.FrogproofingStatus;

	public Tooltip FrogproofingTooltip
		=> new TTGlossary($"status.{Instance.FrogproofingStatus.Id!.Value}");

	public ExternalStatus SmugStatus
		=> Instance.SmugStatus;

	public Tooltip GetSmugTooltip()
		=> new TTGlossary($"status.{Instance.SmugStatus.Id!.Value}");

	public Tooltip GetSmugTooltip(State state, Ship ship)
		=> GetSmugTooltip();

	public int GetMinSmug(Ship ship)
		=> -BotchChances.Length / 2;

	public int GetMaxSmug(Ship ship)
		=> BotchChances.Length / 2;

	public int? GetSmug(Ship ship)
	{
		if (ship.Get((Status)Instance.SmuggedStatus.Id!.Value) <= 0)
			return null;
		return ship.Get((Status)Instance.SmugStatus.Id!.Value);
	}

	public bool IsOversmug(Ship ship)
	{
		var smug = GetSmug(ship);
		return smug is not null && smug.Value > GetMaxSmug(ship);
	}

	public double GetSmugBotchChance(Ship ship)
	{
		var smug = GetSmug(ship);
		if (smug is null)
			return 0;
		else if (smug.Value < GetMinSmug(ship))
			return BotchChances[0];
		else if (smug.Value > GetMaxSmug(ship))
			return 1; // oversmug
		else
			return BotchChances[smug.Value - GetMinSmug(ship)];
	}

	public double GetSmugDoubleChance(Ship ship)
	{
		var smug = GetSmug(ship);
		if (smug is null)
			return 0;
		else if (smug.Value < GetMinSmug(ship))
			return DoubleChances[0];
		else if (smug.Value > GetMaxSmug(ship))
			return 0; // oversmug
		else
			return DoubleChances[smug.Value - GetMinSmug(ship)];
	}

	public int GetTimesBotchedThisCombat(State state, Combat combat)
		=> state.ship.Get((Status)Instance.BotchesStatus.Id!.Value);

	public void RegisterSmugHook(ISmugHook hook, double priority)
		=> Instance.SmugStatusManager.Register(hook, priority);

	public void UnregisterSmugHook(ISmugHook hook)
		=> Instance.SmugStatusManager.Unregister(hook);

	public bool IsFrogproof(Card card)
		=> card is ChipShot;

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> Instance.FrogproofManager.IsFrogproof(state, combat, card, context);

	public void RegisterFrogproofHook(IFrogproofHook hook, double priority)
		=> Instance.FrogproofManager.Register(hook, priority);

	public void UnregisterFrogproofHook(IFrogproofHook hook)
		=> Instance.FrogproofManager.Unregister(hook);
}
